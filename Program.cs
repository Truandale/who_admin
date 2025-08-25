using System.Collections.Concurrent;
using System.ComponentModel;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace who_admin
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.Run(new Form1());
        }
    }

    // Вспомогательные классы для передачи данных
    class ResultRow
    {
        public string Computer { get; set; } = "";
        public string Status { get; set; } = "";
        public string MemberType { get; set; } = "";
        public string Account { get; set; } = "";
        public string Source { get; set; } = "";
        public string ExpandedFrom { get; set; } = "";
    }

    class MemberInfo
    {
        public string Type { get; set; } = "";
        public string Account { get; set; } = "";
    }

    static class LocalAdminsReader
    {
        // --- WinAPI enums/structs ---
        enum SID_NAME_USE : int
        {
            SidTypeUser = 1, SidTypeGroup, SidTypeDomain, SidTypeAlias, SidTypeWellKnownGroup,
            SidTypeDeletedAccount, SidTypeInvalid, SidTypeUnknown, SidTypeComputer, SidTypeLabel
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct LOCALGROUP_MEMBERS_INFO_2
        {
            public IntPtr lgrmi2_sid;
            public SID_NAME_USE lgrmi2_sidusage;
            [MarshalAs(UnmanagedType.LPWStr)] public string lgrmi2_domainandname; // "DOMAIN\\Name"
        }

        // --- P/Invoke ---
        [DllImport("Netapi32.dll", CharSet = CharSet.Unicode)]
        static extern int NetLocalGroupGetMembers(
            string servername, string localgroupname, int level,
            out IntPtr bufptr, int prefmaxlen,
            out int entriesread, out int totalentries, IntPtr resume_handle);

        [DllImport("Netapi32.dll")] static extern int NetApiBufferFree(IntPtr Buffer);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        static extern bool LookupAccountSid(
            string lpSystemName, IntPtr Sid,
            StringBuilder Name, ref uint cchName,
            StringBuilder ReferencedDomainName, ref uint cchReferencedDomainName,
            out SID_NAME_USE peUse);

        // --- Public API ---
        public static IEnumerable<(string Account, string Type, string ExpandedFrom)> GetMembers(string computer)
        {
            // 1) Получаем локализованное имя группы "Администраторы" по её SID
            string adminsLocalName = GetLocalGroupNameBySid(computer, "S-1-5-32-544");

            // 2) Читаем всех членов через NetLocalGroupGetMembers Level=2
            IntPtr buf = IntPtr.Zero;
            int entries, total;
            int status = NetLocalGroupGetMembers(@"\\" + computer, adminsLocalName, 2, out buf, -1, out entries, out total, IntPtr.Zero);
            if (status != 0) throw new Win32Exception(status);

            try
            {
                int sz = Marshal.SizeOf<LOCALGROUP_MEMBERS_INFO_2>();
                for (int i = 0; i < entries; i++)
                {
                    var item = Marshal.PtrToStructure<LOCALGROUP_MEMBERS_INFO_2>(IntPtr.Add(buf, i * sz));
                    string acc = item.lgrmi2_domainandname ?? SidToString(item.lgrmi2_sid);
                    string type = GetMemberTypeDescription(item.lgrmi2_sidusage, acc, computer);
                    yield return (acc, type, "");
                }
            }
            finally { NetApiBufferFree(buf); }
        }

        // --- Helpers ---
        static string GetLocalGroupNameBySid(string computer, string sidStr)
        {
            var sid = new System.Security.Principal.SecurityIdentifier(sidStr);
            byte[] buf = new byte[sid.BinaryLength];
            sid.GetBinaryForm(buf, 0);
            IntPtr pSid = Marshal.AllocHGlobal(buf.Length);
            try
            {
                Marshal.Copy(buf, 0, pSid, buf.Length);
                uint cchName = 0, cchDom = 0;
                LookupAccountSid(computer, pSid, null!, ref cchName, null!, ref cchDom, out _);
                var name = new StringBuilder((int)cchName);
                var dom = new StringBuilder((int)cchDom);
                if (!LookupAccountSid(computer, pSid, name, ref cchName, dom, ref cchDom, out _))
                    throw new Win32Exception(Marshal.GetLastWin32Error());
                return name.ToString();
            }
            finally { Marshal.FreeHGlobal(pSid); }
        }

        static string SidToString(IntPtr sid)
        {
            // На случай "битых" записей без домена\имени
            var si = new System.Security.Principal.SecurityIdentifier(sid);
            return si.Value;
        }

        static string GetMemberTypeDescription(SID_NAME_USE sidUsage, string account, string computer)
        {
            // Определяем, локальная это учётка или доменная
            bool isLocal = account.StartsWith(computer + "\\", StringComparison.OrdinalIgnoreCase);

            string baseType = sidUsage switch
            {
                SID_NAME_USE.SidTypeUser => isLocal ? "Локальный пользователь" : "Доменный пользователь",
                SID_NAME_USE.SidTypeGroup => isLocal ? "Локальная группа" : "Доменная группа",
                SID_NAME_USE.SidTypeAlias => isLocal ? "Локальный алиас" : "Доменный алиас",
                SID_NAME_USE.SidTypeWellKnownGroup => "Встроенная группа",
                SID_NAME_USE.SidTypeComputer => "Компьютер домена",
                _ => "Неизвестный тип"
            };

            // Для доменных пользователей пытаемся получить статус через AD
            if (sidUsage == SID_NAME_USE.SidTypeUser && !isLocal)
            {
                try
                {
                    string statusInfo = GetUserAccountStatus(account);
                    if (!string.IsNullOrEmpty(statusInfo))
                        return $"{baseType} {statusInfo}";
                }
                catch { /* игнорируем ошибки получения статуса */ }
            }

            return baseType;
        }

        static string GetUserAccountStatus(string domainUser)
        {
            try
            {
                var parts = domainUser.Split('\\');
                if (parts.Length != 2) return "";

                string domain = parts[0];
                string username = parts[1];

                using (var ctx = new PrincipalContext(ContextType.Domain, domain))
                using (var user = UserPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, username))
                {
                    if (user == null) return "";

                    var flags = new List<string>();
                    if (user.Enabled == false) flags.Add("ОТКЛЮЧЁН");
                    if (user.IsAccountLockedOut()) flags.Add("ЗАБЛОКИРОВАН");
                    if (user.PasswordNeverExpires == true) flags.Add("ПАРОЛЬ НЕ ИСТЕКАЕТ");

                    return flags.Count > 0 ? $"[{string.Join(", ", flags)}]" : "";
                }
            }
            catch
            {
                return "";
            }
        }
    }

    static class ScanLogic
    {
        public static List<ResultRow> ScanComputer(string computer, bool expandDomainGroups)
        {
            var results = new List<ResultRow>();

            // Используем NetAPI32 вместо WMI для более надёжного получения членов группы
            var directMembers = new List<(string Account, string Type)>();
            
            try
            {
                var members = LocalAdminsReader.GetMembers(computer);
                foreach (var member in members)
                {
                    directMembers.Add((member.Account, member.Type));
                    
                    results.Add(new ResultRow
                    {
                        Computer = computer,
                        Status = "OK",
                        MemberType = member.Type,
                        Account = member.Account,
                        Source = "NetAPI32",
                        ExpandedFrom = ""
                    });
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка NetAPI32: {ex.Message}", ex);
            }

            if (expandDomainGroups)
            {
                // Разворачиваем только доменные группы
                foreach (var member in directMembers.Where(x => x.Type.Contains("Доменная группа")))
                {
                    try
                    {
                        // Извлекаем домен и имя группы из "DOMAIN\GroupName"
                        var parts = member.Account.Split('\\');
                        if (parts.Length == 2)
                        {
                            string domain = parts[0];
                            string groupName = parts[1];
                            
                            var expandedMembers = ExpandDomainGroupMembersSafe(domain, groupName);
                            foreach (var memberInfo in expandedMembers)
                            {
                                results.Add(new ResultRow
                                {
                                    Computer = computer,
                                    Status = "OK",
                                    MemberType = memberInfo.Type,
                                    Account = memberInfo.Account,
                                    Source = "AD (recursive)",
                                    ExpandedFrom = member.Account
                                });
                            }
                        }
                    }
                    catch
                    {
                        // Игнорируем ошибки разворачивания отдельных групп
                    }
                }
            }

            return results;
        }

        static List<MemberInfo> ExpandDomainGroupMembersSafe(string domainNetbios, string groupSam)
        {
            var results = new List<MemberInfo>();
            try
            {
                using (var ctx = new PrincipalContext(ContextType.Domain, domainNetbios))
                using (var gp = GroupPrincipal.FindByIdentity(ctx, IdentityType.SamAccountName, groupSam))
                {
                    if (gp == null) return results;
                    foreach (var p in gp.GetMembers(true))
                    {
                        try
                        {
                            string memberType = "Неизвестный тип";
                            string account = "";
                            
                            if (p is UserPrincipal up)
                            {
                                memberType = "Доменный пользователь (из группы)";
                                string dom = up.Context?.Name ?? domainNetbios;
                                string sam = up.SamAccountName ?? up.Name ?? "";
                                account = $"{dom}\\{sam}";
                            }
                            else if (p is GroupPrincipal grp)
                            {
                                memberType = "Доменная группа (вложенная)";
                                string dom = grp.Context?.Name ?? domainNetbios;
                                string sam = grp.SamAccountName ?? grp.Name ?? "";
                                account = $"{dom}\\{sam}";
                            }
                            else if (p is ComputerPrincipal comp)
                            {
                                memberType = "Компьютер домена";
                                string dom = comp.Context?.Name ?? domainNetbios;
                                string sam = comp.SamAccountName ?? comp.Name ?? "";
                                account = $"{dom}\\{sam}";
                            }

                            if (!string.IsNullOrEmpty(account))
                            {
                                results.Add(new MemberInfo { Type = memberType, Account = account });
                            }
                        }
                        catch { /* пропускаем сбойные объекты */ }
                        finally { p.Dispose(); }
                    }
                }
            }
            catch
            {
                // нет доступа к AD или группа неразрешима — молча пропускаем
            }
            return results;
        }
    }
}
