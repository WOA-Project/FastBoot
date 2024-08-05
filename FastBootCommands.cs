/*
* MIT License
* 
* Copyright (c) 2024 The DuoWOA authors
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace FastBoot
{
    public static class FastBootCommands
    {
        /*
         * Supported commands on Surface Duo (1st Gen) LinuxLoader (ShipMode ON):
         * 
         *  boot                                       DONE
         *  continue                                   DONE
         *  download:                                  DONE
         *  erase:                                     DONE
         *  flash:                                     DONE (TODO: Verify if large files are ok, very unsure here)
         *  flashing get_unlock_ability                DONE
         *  flashing lock                              DONE
         *  flashing unlock                            DONE
         *  getvar:                                    DONE
         *  reboot-bootloader                          DONE
         *  reboot                                     DONE
         *  set_active                                 DONE
         *  snapshot-update
         *  oem battery-rnrmode
         *  oem battery-shipmode
         *  oem clear-devinforollback
         *  oem clear-display-factory-kernel-param
         *  oem clear-mfg-mode
         *  oem clear-sfpd-tamper
         *  oem del-boot-prop
         *  oem device-info
         *  oem disable_act
         *  oem disable-charger-screen
         *  oem enable_act
         *  oem enable-charger-screen
         *  oem get-boot-prop
         *  oem get-display-factory-mode
         *  oem get-mfg-blob
         *  oem get-mfg-mode
         *  oem get-mfg-values
         *  oem get-oem-keys
         *  oem get-sfpd-tamper
         *  oem get-soc-serial
         *  oem select-display-panel
         *  oem set-boot-prop
         *  oem set-display-factory-kernel-param
         *  oem set-oem-keys
         *  oem set-successful
         *  oem show-devinfo
         *  oem show-hw-devinfo
         *  oem touch-fw-version
        */

        public static bool BootImageIntoRam(this FastBootTransport fastBootTransport, Stream stream)
        {
            FastBootStatus status;

            try
            {
                (status, string _, byte[] _) = fastBootTransport.SendData(stream);
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand("boot");
                (status, string _, byte[] _) = responses.Last();
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            return true;
        }

        public static bool BootImageIntoRam(this FastBootTransport fastBootTransport, string filePath)
        {
            using FileStream fileStream = File.OpenRead(filePath);
            return BootImageIntoRam(fastBootTransport, fileStream);
        }

        public static bool GetVariable(this FastBootTransport fastBootTransport, string variable, out string response)
        {
            bool result = GetVariable(fastBootTransport, variable, out string[] responses);
            response = string.Join("\n", responses);
            return result;
        }

        public static bool GetVariable(this FastBootTransport fastBootTransport, string variable, out string[] response)
        {
            (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand($"getvar:{variable}");
            (FastBootStatus status, string _, byte[] _) = responses.Last();

            response = responses.Select(response => response.response).ToArray();

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            return true;
        }

        public static bool GetAllVariables(this FastBootTransport fastBootTransport, out string variablesResponse)
        {
            return GetVariable(fastBootTransport, "all", out variablesResponse);
        }

        public static bool GetAllVariables(this FastBootTransport fastBootTransport, out string[] variablesResponses)
        {
            return GetVariable(fastBootTransport, "all", out variablesResponses);
        }

        public static bool GetAllVariables(this FastBootTransport fastBootTransport, out (string, string)[] variables)
        {
            bool result = GetAllVariables(fastBootTransport, out string[] responses);

            // Edge cases:
            // vendor-fingerprint
            // system-fingerprint

            List<(string, string)> variableList = new();

            foreach (string response in responses.Where(t => t.Contains(':')))
            {
                if (response.StartsWith("vendor-fingerprint:") || response.StartsWith("system-fingerprint:"))
                {
                    string variableName = response.Split(":")[0];
                    string variableValue = string.Join(":", response.Split(":").Skip(1));
                    variableList.Add((variableName, variableValue));
                }
                else
                {
                    string variableName = response.Substring(0, response.LastIndexOf(":"));
                    string variableValue = response.Substring(response.LastIndexOf(":") + 1);
                    variableList.Add((variableName, variableValue));
                }
            }

            variables = variableList.ToArray();

            return result;
        }

        public static bool ContinueBoot(this FastBootTransport fastBootTransport)
        {
            FastBootStatus status;

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand("continue");
                (status, string _, byte[] _) = responses.Last();
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            return true;
        }

        public static bool Reboot(this FastBootTransport fastBootTransport, string mode)
        {
            FastBootStatus status;

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand($"reboot-{mode}");
                (status, string _, byte[] _) = responses.Last();
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            return true;
        }

        public static bool Reboot(this FastBootTransport fastBootTransport)
        {
            FastBootStatus status;

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand($"reboot");
                (status, string _, byte[] _) = responses.Last();
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            return true;
        }

        public static bool RebootRecovery(this FastBootTransport fastBootTransport)
        {
            return Reboot(fastBootTransport, "recovery");
        }

        public static bool RebootFastBootD(this FastBootTransport fastBootTransport)
        {
            return Reboot(fastBootTransport, "fastboot");
        }

        public static bool RebootBootloader(this FastBootTransport fastBootTransport)
        {
            return Reboot(fastBootTransport, "bootloader");
        }

        public static bool FlashPartition(this FastBootTransport fastBootTransport, string partition, Stream stream)
        {
            FastBootStatus status;

            try
            {
                (status, string _, byte[] _) = fastBootTransport.SendData(stream);
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand($"flash:{partition}");
                (status, string _, byte[] _) = responses.Last();
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            return true;
        }

        public static bool FlashPartition(this FastBootTransport fastBootTransport, string partition, string filePath)
        {
            using FileStream fileStream = File.OpenRead(filePath);
            return FlashPartition(fastBootTransport, partition, fileStream);
        }

        public static bool ErasePartition(this FastBootTransport fastBootTransport, string partition)
        {
            FastBootStatus status;

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand($"erase:{partition}");
                (status, string _, byte[] _) = responses.Last();
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            return true;
        }

        public static bool PowerDown(this FastBootTransport fastBootTransport)
        {
            FastBootStatus status;

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand($"powerdown");
                (status, string _, byte[] _) = responses.Last();
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            return true;
        }

        public static bool SetActive(this FastBootTransport fastBootTransport, string slot)
        {
            FastBootStatus status;

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand($"set_active:{slot}");
                (status, string _, byte[] _) = responses.Last();
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            return true;
        }

        public static bool SetActiveOther(this FastBootTransport fastBootTransport)
        {
            bool result = GetVariable(fastBootTransport, "current-slot", out string CurrentSlot);
            if (!result)
            {
                return false;
            }

            if (CurrentSlot == "a")
            {
                return SetActiveB(fastBootTransport);
            }

            if (CurrentSlot == "b")
            {
                return SetActiveA(fastBootTransport);
            }

            return false;
        }

        public static bool SetActiveA(this FastBootTransport fastBootTransport)
        {
            return SetActive(fastBootTransport, "a");
        }

        public static bool SetActiveB(this FastBootTransport fastBootTransport)
        {
            return SetActive(fastBootTransport, "b");
        }

        public static bool FlashingGetUnlockAbility(this FastBootTransport fastBootTransport, out bool canUnlock)
        {
            FastBootStatus status;
            string response;
            canUnlock = false;

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand($"flashing get_unlock_ability");
                (FastBootStatus _, response, byte[] _) = responses.First();
                (status, string _, byte[] _) = responses.Last();
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            // Sample response:
            // get_unlock_ability: 1

            if (response.StartsWith("get_unlock_ability: "))
            {
                canUnlock = response.EndsWith('1');
                return true;
            }

            return false;
        }

        public static bool FlashingLock(this FastBootTransport fastBootTransport)
        {
            FastBootStatus status;

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand($"flashing lock");
                (status, string _, byte[] _) = responses.Last();
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            return true;
        }

        public static bool FlashingUnlock(this FastBootTransport fastBootTransport)
        {
            FastBootStatus status;

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand($"flashing unlock");
                (status, string _, byte[] _) = responses.Last();
            }
            catch
            {
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                return false;
            }

            return true;
        }

        public static bool SendOEMCommand(this FastBootTransport fastBootTransport, string command, out string commandResponse)
        {
            FastBootStatus status;
            string response = "";

            try
            {
                (FastBootStatus status, string response, byte[] rawResponse)[] responses = fastBootTransport.SendCommand($"oem {command}");
                (status, string _, byte[] _) = responses.Last();
                foreach((FastBootStatus status, string response, byte[] rawResponse) responseLine in responses) 
                {
                    if (responseLine.response.EndsWith("\n") || responseLine.response.EndsWith("\r\n"))
                    {
                        response += responseLine.response;
                    }
                    else
                    {
                        response += responseLine.response + "\n";
                    }
                }
                response = response.Trim(); // Trim any unnecessary whitespaces.
            }
            catch
            {
                commandResponse = response;
                return false;
            }

            if (status != FastBootStatus.OKAY)
            {
                commandResponse = response; // The OEM Command may fail with a specific response that the end user may want to see, still assign a response.
                return false;
            }

            commandResponse = response;

            return true;
        }
    }
}
