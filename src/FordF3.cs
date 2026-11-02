// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Rapidfire
using System;using System.IO;using System.Windows.Forms;
partial class BurnModern {
    internal static byte[] FordPacket(char operation,int address,byte[] image,int start,int count){
        if(operation=='E'){if(address<0||address>3)throw new ArgumentOutOfRangeException();return new byte[]{(byte)'J',(byte)'E',(byte)address,(byte)('J'+'E'+address)};}
        if(operation!='W'||image==null||count<1||count>256||start<0||start+count>image.Length||address<0||address+count>262144)throw new ArgumentException("Invalid F3 packet.");
        var cmd=new byte[7+count];cmd[0]=(byte)'J';cmd[1]=(byte)'W';cmd[2]=(byte)count;cmd[3]=(byte)(address>>16);cmd[4]=(byte)(address>>8);cmd[5]=(byte)address;Array.Copy(image,start,cmd,6,count);int sum=0;for(int i=0;i<cmd.Length-1;i++)sum+=cmd[i];cmd[cmd.Length-1]=(byte)sum;return cmd;
    }
    async System.Threading.Tasks.Task RunFord(bool erase){
        if(!erase&&(data==null||data.Length!=262144||offset.Value!=0)){MessageBox.Show("Original F3 programming requires a complete 262,144-byte linear BIN at address zero. Compact 216/224 KB and 32/56/64 KB Ford files need a separate correctly mapped full image; no automatic conversion is performed.");return;}
        if(MessageBox.Show(erase?"Erase the physically selected ORIGINAL F3 slot through F2A, then read all 256 KB twice to blank-check? Only the selected slot will be erased. Hardware test pending.":"Program the loaded full 256 KB image to the physically selected ORIGINAL F3 slot? Requires blank slot; no automatic erase. Two complete readbacks will verify the result. Hardware test pending.","Original F3 slot operation",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
        string portName=connectedPort;byte[] image=erase?null:(byte[])data.Clone();hardwareBusy=true;UpdateChipControls();chips.Enabled=false;ports.Enabled=false;status.Text="●  F3 WORKING";var progress=new Progress<string>(message=>Log(message));
        try{string result=await System.Threading.Tasks.Task.Run(()=>FordOperation(portName,image,erase,progress));Log(result);status.Text=erase?"●  BLANK":"●  VERIFIED";MessageBox.Show(result,"Original F3 result");}
        catch(Exception ex){status.Text="●  F3 ERROR";Log("F3 operation failed: "+ex.Message);MessageBox.Show(ex.Message+"\r\nNo automatic retry. Slot contents may have changed.","Original F3 result");}
        finally{hardwareBusy=false;chips.Enabled=true;ports.Enabled=true;UpdateChipControls();}
    }
    static string FordOperation(string name,byte[] image,bool erase,IProgress<string> progress){
        if(!erase){progress.Report("F3: reading selected 256 KB slot twice before writing.");var before=ReadTest(name,true,262144,0,'J');int first,count;if(!CheckBlank(before,out first,out count))throw new IOException("Selected F3 slot is not blank; no writes sent.");}
        using(var port=OpenBurn(name)){port.ReadTimeout=5000;if(erase){for(int bank=0;bank<4;bank++){var cmd=FordPacket('E',bank,null,0,0);port.Write(cmd,0,cmd.Length);if(port.ReadByte()!='O')throw new IOException("F3 bank erase rejected: "+bank);}}else{for(int address=0;address<262144;address+=256){var cmd=FordPacket('W',address,image,address,256);port.Write(cmd,0,cmd.Length);if(port.ReadByte()!='O')throw new IOException("F3 write rejected at 0x"+address.ToString("X6"));if(address%65536==0)progress.Report("F3 write acknowledged through 0x"+(address+255).ToString("X6"));}}}
        progress.Report("F3: two full selected-slot readbacks and checksums.");var actual=ReadTest(name,true,262144,0,'J');for(int i=0;i<actual.Length;i++)if(actual[i]!=(erase?(byte)255:image[i]))throw new IOException((erase?"F3 blank check failed":"F3 verify failed")+" at 0x"+i.ToString("X6"));
        return erase?"BLANK: selected F3 slot has 262,144 FF bytes in two matching checked reads. Other physical switch slots were not accessed.":"VERIFIED: all 262,144 loaded BIN bytes match two full checked reads of the selected F3 slot. Other physical switch slots were not accessed.";
    }
}
