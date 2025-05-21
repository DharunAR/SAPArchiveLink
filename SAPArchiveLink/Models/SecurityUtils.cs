using System.Net.Security;

namespace SAPArchiveLink
{
    public static class SecurityUtils
    {
        public static bool NeedsSignature(ALCommand command, int serverProtectionLevel)
        {
            int accMode = AccessModeToInt(command.GetAccessMode());
            bool result = (accMode & serverProtectionLevel) > 0;
            return result;
        }

        public static int AccessModeToInt(char? mode)
        {
            int accMode = ALCommand.PROT_NO_MAX;
            if (!mode.HasValue)
            {
                accMode = 0;
            }
            else
            {
                accMode = mode.Value switch
                {
                    ALCommand.PROT_READ => ALCommand.PROT_NO_READ,
                    ALCommand.PROT_CREATE => ALCommand.PROT_NO_CREATE,
                    ALCommand.PROT_UPDATE => ALCommand.PROT_NO_UPDATE,
                    ALCommand.PROT_DELETE => ALCommand.PROT_NO_DELETE,
                    ALCommand.PROT_ELIB => ALCommand.PROT_NO_ELIB,
                    ALCommand.PROT_NONE => ALCommand.PROT_NO_NONE,
                    _ => 0
                };
            }
            return accMode;
        }
    }

}
