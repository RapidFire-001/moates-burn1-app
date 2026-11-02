// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Rapidfire
using System;
using System.Drawing;
using System.IO;
using System.IO.Ports;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Drawing.Drawing2D;

partial class BurnModern : Form {
    Color bg=Color.FromArgb(17,19,16), panel=Color.FromArgb(25,28,24), muted=Color.FromArgb(150,156,145), accent=Color.FromArgb(194,243,107);
    Label fileName, details, status; TextBox hex, log; ComboBox chips, ports; NumericUpDown offset;
    byte[] data; Button eraseControl,programControl,verifyControl,readControl,disconnectControl,blankControl; Label chipHelp,selectionNotice; string loadedFile="BIN",connectedPort; bool restoringProfile;
    readonly string preferencePath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"BurnModern.settings");
    public BurnModern() {
        Text="BURN / Device Studio v21"; Size=new Size(1120,790); MinimumSize=new Size(1040,700); StartPosition=FormStartPosition.CenterScreen;
        using(var stream=typeof(BurnModern).Assembly.GetManifestResourceStream("BurnModern.ChipIcon")){if(stream!=null)using(var chipIcon=new Icon(stream)){Icon=(Icon)chipIcon.Clone();}}
        DoubleBuffered=true;
        BackColor=bg; ForeColor=Color.White; Font=new Font("Segoe UI",10);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(30),ColumnCount=1,RowCount=5};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,86));root.RowStyles.Add(new RowStyle(SizeType.Absolute,76));root.RowStyles.Add(new RowStyle(SizeType.Absolute,125));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,115)); Controls.Add(root);
        var header=new Panel{Dock=DockStyle.Fill}; header.Controls.Add(Label("BURN / Modern",0,0,350,38,24,Color.White));header.Controls.Add(Label("A clearer workspace for your Moates BURN1",2,45,650,25,11,muted));root.Controls.Add(header);
        var connection=new FlowLayoutPanel{Dock=DockStyle.Fill,BackColor=panel,Padding=new Padding(14),WrapContents=false};
        status=Label("●  OFFLINE",0,0,130,35,11,accent); connection.Controls.Add(status);
        ports=new ComboBox{Width=150,DropDownStyle=ComboBoxStyle.DropDownList};connection.Controls.Add(ports);
        var refresh=Button("Refresh ports",125);refresh.Click+=(s,e)=>RefreshPorts();connection.Controls.Add(refresh);
        var identify=Button("Connect",110); identify.Click+=async(s,e)=>{if(hardwareBusy)return;if(ports.SelectedItem==null){Log("Select a COM port first.");return;}string selected=ports.SelectedItem.ToString();connectedPort=null;hardwareBusy=true;UpdateChipControls();chips.Enabled=false;ports.Enabled=false;identify.Enabled=false;UpdateChipControls();status.Text="●  CHECKING";try{string result=await System.Threading.Tasks.Task.Run(()=>Identify(selected));Log(result);bool connected=result.StartsWith("BURN");connectedPort=connected?selected:null;status.Text=connected?"●  CONNECTED":"●  UNCONFIRMED";if(connected)RestoreLastProfile();MessageBox.Show(result,"Programmer connection");}catch(Exception ex){connectedPort=null;status.Text="●  OFFLINE";Log(ex.Message);MessageBox.Show(ex.Message,"Connection failed");}finally{hardwareBusy=false;chips.Enabled=true;ports.Enabled=true;identify.Enabled=true;UpdateChipControls();}};connection.Controls.Add(identify);
        disconnectControl=Button("Disconnect",110);disconnectControl.Enabled=false;disconnectControl.Click+=(s,e)=>DisconnectSession();connection.Controls.Add(disconnectControl);
        selectionNotice=Label("Select chip manually",0,0,250,35,9,muted);connection.Controls.Add(selectionNotice);root.Controls.Add(connection);
        var filePanel=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=1,Padding=new Padding(0,18,0,0)};
        filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,170));
        var fileText=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=1,RowCount=2,Margin=new Padding(0)};
        fileText.RowStyles.Add(new RowStyle(SizeType.Absolute,36));fileText.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        fileName=Label("Load your binary",0,0,400,32,17,Color.White);fileName.Dock=DockStyle.Fill;fileName.AutoEllipsis=true;
        details=Label("Open a .bin file to inspect its contents and chip placement.",0,0,400,40,10,muted);details.Dock=DockStyle.Fill;details.AutoEllipsis=true;
        fileText.Controls.Add(fileName);fileText.Controls.Add(details);filePanel.Controls.Add(fileText,0,0);
        var open=Button("Open BIN file",150);open.Anchor=AnchorStyles.Top|AnchorStyles.Right;open.Click+=(s,e)=>OpenBin();filePanel.Controls.Add(open,1,0);filePanel.ColumnCount=3;filePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,140));var stack=Button("ROM stacker",130);stack.Click+=(s,e)=>{if(hardwareBusy){MessageBox.Show("Wait for the chip operation to finish.");return;}using(var tool=new RomStacker())tool.ShowDialog(this);};filePanel.Controls.Add(stack,2,0);root.Controls.Add(filePanel);
        KeyPreview=true;KeyDown+=(s,e)=>{if(e.Control&&e.KeyCode==Keys.O){e.SuppressKeyPress=true;OpenBin();}};
        var workspace=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,64));workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,36));
        hex=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Both,WordWrap=false,BackColor=panel,ForeColor=Color.FromArgb(188,218,231),BorderStyle=BorderStyle.None,Font=new Font("Consolas",10),Text="\r\n  HEX PREVIEW\r\n\r\n  Your binary will appear here.\r\n\r\n  Address    Bytes                                             ASCII"};workspace.Controls.Add(hex);
        var settings=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,Padding=new Padding(20,0,0,0)};
        settings.Controls.Add(Label("CHIP SETUP",0,0,280,30,10,muted));chips=new ComboBox{Width=280,DropDownStyle=ComboBoxStyle.DropDownList};chips.Items.AddRange(new object[]{"SST27SF512 — 64 KB","AT29C256 — 32 KB","AM29F040 — 512 KB"});chips.Items.Add("Winbond W27C512 — experimental read only");chips.SelectedIndex=3;settings.Controls.Add(chips);
        settings.Controls.Add(Label("Chip start address (hex)",0,0,280,28,10,muted));offset=new NumericUpDown{Width=280,Hexadecimal=true,Maximum=524287,BackColor=panel,ForeColor=Color.White};settings.Controls.Add(offset);
        var align=Button("Place at end of chip",280);align.Click+=(s,e)=>{if(data==null){MessageBox.Show("Open a BIN file first.");return;}if(data.Length>Capacity()){MessageBox.Show("This file is larger than the selected chip.");return;}offset.Value=Capacity()-data.Length;ValidatePlacement();};settings.Controls.Add(align);
        var check=Button("Check placement",280);check.Click+=(s,e)=>ValidatePlacement();settings.Controls.Add(check);
        chips.Items[0]="SST27SF512 — 64 KB · testing pending";chips.Items[1]="AT29C256 — 32 KB · OneROM read passed";chips.Items[2]="AM29F040 — 512 KB · testing pending";
        chips.Items[3]="W27C512 — no OneROM test; erase failed";
        chips.Items.Add("SST39SF040 profile — 512 KB · tested chip passed");chips.SelectedIndex=4;
        chips.Items.AddRange(new object[]{"27C128 — 16 KB · OneROM read passed","27C256 — 32 KB · OneROM read passed","27C512 — 64 KB · OneROM read passed"});chips.Items.AddRange(new object[]{"2732A / 27C32 — 4 KB · read only, untested","AM29F010 — 128 KB · experimental read only","Ford F3 original + F2A — 256 KB slot · untested","SST39SF010A — 128 KB · experimental read only"});chips.DropDownWidth=490;
        var read=Button("Read chip twice + save backup",280);readControl=read;read.Click+=async(s,e)=>{
            if(hardwareBusy)return;
            if(!EnsureConnection())return;
            int profile=chips.SelectedIndex;char mode=ReadMode(profile);
            bool sst=profile==4||profile==2||profile==9||profile==10||profile==11;string chipName=new[]{"SST27SF512","AT29C256","AM29F040","W27C512","SST39SF040-profile","27C128","27C256","27C512","2732A","AM29F010","Ford-F3-original","SST39SF010A"}[profile];
            int readSize=Capacity();int readBase=chips.SelectedIndex==5?0xC000:chips.SelectedIndex==6?0x8000:0;
            if(MessageBox.Show("Read "+chipName+" ("+readSize.ToString("N0")+" bytes). "+(profile==10?"Confirm original F3 through F2A, removed from ECU, with the intended slot physically selected. ":"Confirm the correct chip is seated, with its notch toward the ZIF lever; for 28-pin chips the four unused socket positions are nearest the lever. ")+"This reads twice; no erase or program commands are sent.","Read-only chip test",MessageBoxButtons.OKCancel)!=DialogResult.OK)return;
            string selected=connectedPort;Log("Reading selected chip through connected port "+selected+".");read.Enabled=false;chips.Enabled=false;identify.Enabled=false;refresh.Enabled=false;ports.Enabled=false;hardwareBusy=true;UpdateChipControls();status.Text="●  READING";
            UpdateChipControls();
            try{byte[] backup=await System.Threading.Tasks.Task.Run(()=>ReadTest(selected,sst,readSize,readBase,mode));string digest;using(var sha=SHA256.Create())digest=BitConverter.ToString(sha.ComputeHash(backup)).Replace("-","");int ff=0,zero=0;foreach(byte b in backup){if(b==255)ff++;if(b==0)zero++;}
                string result="Two complete "+backup.Length.ToString("N0")+"-byte reads match. SHA-256: "+digest+". "+(ff==backup.Length?"All bytes are FF: could be blank or an unresponsive/poorly seated chip.":zero==backup.Length?"All bytes are 00: check chip seating; this does not prove a working chip.":"Data is stable; this does not establish erase/program compatibility.");Log(result);status.Text="●  READ MATCH";
                using(var save=new SaveFileDialog{Filter="Binary backup (*.bin)|*.bin",FileName=chipName+"-backup-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+".bin",Title="Save matching chip reads"}){if(save.ShowDialog(this)==DialogResult.OK){File.WriteAllBytes(save.FileName,backup);File.WriteAllText(save.FileName+".txt",chipName+Environment.NewLine+result+Environment.NewLine+"Experimental read on "+selected+". No erase or program commands sent.");Log("Backup saved: "+save.FileName);}}
                MessageBox.Show(result,"Read test result");
            }catch(Exception ex){status.Text="●  TEST FAILED";Log(ex.Message);MessageBox.Show(ex.Message,"Read test failed — no backup saved");}
            finally{hardwareBusy=false;read.Enabled=true;chips.Enabled=true;identify.Enabled=true;refresh.Enabled=true;ports.Enabled=true;UpdateChipControls();}
        };settings.Controls.Add(read);
        var erase=Button("Experimental erase + blank check",280);eraseControl=erase;settings.Controls.Add(erase);
        erase.Click+=async(s,e)=>{
            if(hardwareBusy)return;
            if(!EnsureConnection())return;
            if(chips.SelectedIndex==10){await RunFord(true);return;}
            if(chips.SelectedIndex==0||chips.SelectedIndex==2){
                int p=chips.SelectedIndex;string port=connectedPort;
                if(MessageBox.Show("Erase the selected chip, then blank-check the full capacity? This profile is implemented but hardware testing is pending. Chip contents will be lost.","Erase chip",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
                hardwareBusy=true;UpdateChipControls();chips.Enabled=false;read.Enabled=false;ports.Enabled=false;identify.Enabled=false;refresh.Enabled=false;UpdateChipControls();status.Text="●  ERASING";
                try{string result=await System.Threading.Tasks.Task.Run(()=>ExperimentalErase(port,p==2));Log(result);status.Text=result.StartsWith("Blank check passed")?"●  BLANK":"●  NOT BLANK";MessageBox.Show(result,"Erase result");}
                catch(Exception ex){Log("Erase result unconfirmed: "+ex.Message);status.Text="●  FAILED";MessageBox.Show(ex.Message,"Erase result unconfirmed");}
                finally{hardwareBusy=false;chips.Enabled=true;read.Enabled=true;ports.Enabled=true;identify.Enabled=true;refresh.Enabled=true;UpdateChipControls();}return;
            }
            if(chips.SelectedIndex==4){
                if(MessageBox.Show("Try the BURN1 AM29F040 erase commands on this SST39SF040 test chip? Contents may be lost. The sector sizes differ: this may erase only eight 4 KB sectors, not the full chip. A full 512 KB blank check follows. Keep your backup.","Experimental SST39SF040 erase",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
                string sstPort=connectedPort;hardwareBusy=true;UpdateChipControls();read.Enabled=false;erase.Enabled=false;chips.Enabled=false;identify.Enabled=false;refresh.Enabled=false;ports.Enabled=false;status.Text="●  ERASING";
                try{Log("Trying AM29F040 bank erase commands 0–7; full-chip blank check follows.");string result=await System.Threading.Tasks.Task.Run(()=>ExperimentalErase(sstPort,true,true));Log(result);status.Text=result.StartsWith("Blank check passed")?"●  BLANK":"●  NOT BLANK";MessageBox.Show(result,"SST39SF040 erase result");}
                catch(Exception ex){status.Text="●  UNCONFIRMED";Log("Erase/blank check unconfirmed: "+ex.Message);MessageBox.Show("Erase result unconfirmed: "+ex.Message+"\r\nChip contents may have changed. Use the read-only test before continuing.","SST39SF040 erase result");}
                finally{hardwareBusy=false;read.Enabled=true;chips.Enabled=true;identify.Enabled=true;refresh.Enabled=true;ports.Enabled=true;UpdateChipControls();}
                return;
            }
            if(chips.SelectedIndex!=3){MessageBox.Show("Select W27C512 or SST39SF040 for experimental erase.");return;}
            if(MessageBox.Show("This sends the BURN1 SST27SF512 erase command to your W27C512. W27C512 requires different erase conditions, so this is unsupported and may fail or alter the chip contents. Keep your saved backup. Continue with one erase attempt and a read-back blank check?","Experimental erase — chip contents may be lost",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
            string selected=connectedPort;hardwareBusy=true;UpdateChipControls();chips.Enabled=false;read.Enabled=false;erase.Enabled=false;identify.Enabled=false;refresh.Enabled=false;ports.Enabled=false;status.Text="●  ERASING";UpdateChipControls();
            try{Log("Experimental SST erase requested for W27C512. One attempt only.");string result=await System.Threading.Tasks.Task.Run(()=>ExperimentalErase(selected));Log(result);status.Text=result.StartsWith("Blank check passed")?"●  BLANK":"●  NOT BLANK";MessageBox.Show(result,"Experimental erase result");}
            catch(Exception ex){status.Text="●  UNCONFIRMED";Log("Erase/blank check unconfirmed: "+ex.Message);MessageBox.Show("Erase result unconfirmed: "+ex.Message+"\r\nChip contents may have changed. Run the read-only test before any further action.","Experimental erase result");}
            finally{hardwareBusy=false;chips.Enabled=true;read.Enabled=true;identify.Enabled=true;refresh.Enabled=true;ports.Enabled=true;UpdateChipControls();}
        };
        var program=Button("Program BIN + verify",280);programControl=program;settings.Controls.Add(program);
        program.Click+=async(s,e)=>{
            if(hardwareBusy)return;
            if(!EnsureConnection())return;
            if(chips.SelectedIndex==10){await RunFord(false);return;}
            if(chips.SelectedIndex<=2){
                if(data==null){MessageBox.Show("Open the BIN you want to program first (Ctrl+O).");return;}int p=chips.SelectedIndex,start=(int)offset.Value;byte[] standardImage=(byte[])data.Clone();string port=connectedPort;
                if((long)start+standardImage.Length>ProfileSize(p)){MessageBox.Show("The BIN does not fit at this address.");return;}
                if(MessageBox.Show("Program "+standardImage.Length.ToString("N0")+" bytes at 0x"+start.ToString("X6")+" and verify the whole chip? Hardware testing of this profile is pending. "+(p==1?"AT29C256 rewrites 64-byte pages; bytes outside the BIN are preserved.":"A blank chip is required; this action will not erase automatically."),"Program chip",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
                hardwareBusy=true;UpdateChipControls();chips.Enabled=false;read.Enabled=false;ports.Enabled=false;identify.Enabled=false;refresh.Enabled=false;open.Enabled=false;UpdateChipControls();status.Text="●  WORKING";var progressStandard=new Progress<string>(message=>Log(message));
                try{string result=await System.Threading.Tasks.Task.Run(()=>ProgramStandard(port,p,standardImage,start,progressStandard));Log(result);status.Text="●  VERIFIED";MessageBox.Show(result,"Program result");}
                catch(Exception ex){Log("Programming failed: "+ex.Message);status.Text="●  FAILED";MessageBox.Show(ex.Message+"\r\nChip may be partially programmed. No automatic retry.","Program result");}
                finally{hardwareBusy=false;chips.Enabled=true;read.Enabled=true;ports.Enabled=true;identify.Enabled=true;refresh.Enabled=true;open.Enabled=true;UpdateChipControls();}return;
            }
            if(chips.SelectedIndex!=4){MessageBox.Show("Programming is available only for the experimental SST39SF040 profile.");return;}
            if(data==null){MessageBox.Show("Open your BIN file first (Ctrl+O).");return;}
            if(offset.Value!=0){MessageBox.Show("This programming test uses start address 0x000000. Set the address to zero.");return;}
            byte[] image=(byte[])data.Clone();string selected=connectedPort;
            if(MessageBox.Show("Program the loaded BIN ("+image.Length.ToString("N0")+" bytes) at 0x000000 using experimental AM29F040 write commands? The app checks for a blank chip first, then writes and verifies all 512 KB. Unused bytes must remain FF. This may take several minutes. Keep USB connected.","Program SST39SF040 test chip",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)!=DialogResult.Yes)return;
            hardwareBusy=true;UpdateChipControls();program.Enabled=false;read.Enabled=false;erase.Enabled=false;chips.Enabled=false;ports.Enabled=false;identify.Enabled=false;refresh.Enabled=false;open.Enabled=false;status.Text="●  WORKING";
            var progress=new Progress<string>(message=>{Log(message);});
            try{string result=await System.Threading.Tasks.Task.Run(()=>ProgramTest(selected,image,progress));Log(result);status.Text="●  VERIFIED";MessageBox.Show(result,"Program and verify result");}
            catch(Exception ex){status.Text="●  FAILED";Log("Program/verify failed: "+ex.Message);MessageBox.Show("Program/verify failed: "+ex.Message+"\r\nThe chip may be partially programmed. No automatic retry or erase performed.","Program and verify result");}
            finally{hardwareBusy=false;program.Enabled=true;read.Enabled=true;erase.Enabled=true;chips.Enabled=true;ports.Enabled=true;identify.Enabled=true;refresh.Enabled=true;open.Enabled=true;UpdateChipControls();}
        };
        var verify=Button("Verify chip vs BIN",230);verifyControl=verify;
        verify.Click+=async(s,e)=>{
            if(hardwareBusy)return;
            if(!EnsureConnection())return;
            if(data==null){Log("Choose the reference BIN to compare with the chip.");OpenBin();if(data==null)return;}
            int profile=chips.SelectedIndex,start=(int)offset.Value,size=ProfileSize(profile);byte[] fileBytes=(byte[])data.Clone();string filename=loadedFile,port=connectedPort;
            if((long)start+fileBytes.Length>size){MessageBox.Show("The loaded BIN does not fit at the selected chip start address.");return;}
            bool large=profile==2||profile==4||profile==9||profile==10||profile==11;int readBase=profile==5?0xC000:profile==6?0x8000:0;char mode=ReadMode(profile);
            hardwareBusy=true;UpdateChipControls();chips.Enabled=false;ports.Enabled=false;read.Enabled=false;identify.Enabled=false;refresh.Enabled=false;open.Enabled=false;offset.Enabled=false;align.Enabled=false;check.Enabled=false;UpdateChipControls();status.Text="●  VERIFYING";
            Log("Read-only verify on "+port+": "+filename+", "+fileBytes.Length.ToString("N0")+" bytes at chip address 0x"+start.ToString("X6")+". Reading chip twice; no erase or program commands.");
            try{
                byte[] chipBytes=await System.Threading.Tasks.Task.Run(()=>ReadTest(port,large,size,readBase,mode));
                Comparison result=CompareFile(chipBytes,fileBytes,start);string message=result.Describe();Log(message);status.Text=result.Differences==0?"●  MATCH":"●  MISMATCH";MessageBox.Show(message,"Chip vs BIN — "+filename);
            }catch(Exception ex){status.Text="●  VERIFY ERROR";Log("Verification could not complete: "+ex.Message);MessageBox.Show(ex.Message,"Verification could not complete");}
            finally{hardwareBusy=false;chips.Enabled=true;ports.Enabled=true;read.Enabled=true;identify.Enabled=true;refresh.Enabled=true;open.Enabled=true;offset.Enabled=true;align.Enabled=true;check.Enabled=true;UpdateChipControls();}
        };
        var blank=Button("Blank test",180);blankControl=blank;
        blank.Click+=async(s,e)=>{
            if(hardwareBusy||!EnsureConnection())return;
            int profile=chips.SelectedIndex,size=ProfileSize(profile),readBase=profile==5?0xC000:profile==6?0x8000:0;
            bool large=profile==2||profile==4||profile==9||profile==10||profile==11;char mode=ReadMode(profile);string port=connectedPort;
            hardwareBusy=true;UpdateChipControls();chips.Enabled=false;ports.Enabled=false;identify.Enabled=false;refresh.Enabled=false;open.Enabled=false;offset.Enabled=false;align.Enabled=false;check.Enabled=false;status.Text="●  BLANK TEST";
            Log("Read-only blank test: two complete "+size.ToString("N0")+"-byte reads. No erase or program commands; loaded BIN is unchanged.");
            try{byte[] bytes=await System.Threading.Tasks.Task.Run(()=>ReadTest(port,large,size,readBase,mode));int first,count;bool isBlank=CheckBlank(bytes,out first,out count);string message=isBlank?"BLANK: all "+bytes.Length.ToString("N0")+" bytes are FF in two matching checked reads. An all-FF read alone cannot distinguish blank contents from poor contact.":"NOT BLANK: "+count.ToString("N0")+" bytes are not FF; first at chip address 0x"+first.ToString("X6")+" (value "+bytes[first].ToString("X2")+"). Two full reads matched with valid checksums.";Log(message);status.Text=isBlank?"●  BLANK":"●  NOT BLANK";MessageBox.Show(message,"Read-only blank test");}
            catch(Exception ex){status.Text="●  TEST ERROR";Log("Blank test could not complete: "+ex.Message);MessageBox.Show(ex.Message,"Blank test could not complete");}
            finally{hardwareBusy=false;chips.Enabled=true;ports.Enabled=true;identify.Enabled=true;refresh.Enabled=true;open.Enabled=true;offset.Enabled=true;align.Enabled=true;check.Enabled=true;UpdateChipControls();}
        };
        chips.SelectedIndexChanged+=(s,e)=>{UpdateChipControls();if(!restoringProfile){selectionNotice.Text="Manual chip selection";selectionNotice.ForeColor=muted;try{ProfilePreferences.Save(preferencePath,chips.SelectedIndex);}catch(Exception ex){if(log!=null)Log("Could not remember chip profile: "+ex.Message);}}};
        chipHelp=Label("",0,0,285,115,9,muted);settings.Controls.Add(chipHelp);UpdateChipControls();workspace.Controls.Add(settings);root.Controls.Add(workspace);
        log=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=bg,ForeColor=muted,BorderStyle=BorderStyle.None,Font=new Font("Consolas",9)};root.Controls.Add(log);Log("v21 ready. Connect once; chip actions reuse that COM port. Verify opens a reference BIN picker if no file is loaded.");RefreshPorts();RestoreLastProfile();
        ports.SelectedIndexChanged+=(s,e)=>{if(connectedPort!=null&&ports.SelectedItem as string!=connectedPort){connectedPort=null;status.Text="●  OFFLINE";Log("Port selection changed. Connect again before chip operations.");UpdateChipControls();}};
        // Reflow into a single page: settings and actions remain visible above the preview.
        root.SuspendLayout();root.Controls.Clear();root.RowStyles.Clear();root.RowCount=7;root.Padding=new Padding(20);
        foreach(int height in new[]{62,52,80,70,48})root.RowStyles.Add(new RowStyle(SizeType.Absolute,height));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,86));
        header.Controls[0].Height=34;header.Controls[1].Top=34;header.Controls[1].Height=24;
        connection.Padding=new Padding(10,5,0,0);connection.AutoScroll=false;
        filePanel.Padding=new Padding(0,5,0,0);fileText.RowStyles[0].Height=32;
        var setup=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=4,RowCount=2,Margin=new Padding(0)};
        setup.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));setup.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,155));setup.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,185));setup.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,155));setup.RowStyles.Add(new RowStyle(SizeType.Absolute,24));setup.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        setup.Controls.Add(Label("CHIP PROFILE",0,0,200,24,9,muted),0,0);setup.Controls.Add(Label("BIN START (HEX)",0,0,150,24,9,muted),1,0);
        chips.Dock=DockStyle.Fill;chips.Margin=new Padding(0,0,14,0);setup.Controls.Add(chips,0,1);offset.Dock=DockStyle.Fill;offset.Margin=new Padding(0,0,14,0);setup.Controls.Add(offset,1,1);
        align.Text="Place BIN at end";align.Width=175;align.Margin=new Padding(0);setup.Controls.Add(align,2,1);check.Width=145;check.Margin=new Padding(0);setup.Controls.Add(check,3,1);
        var actions=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=5,RowCount=1,Margin=new Padding(0)};for(int i=0;i<5;i++)actions.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,20));
        read.Text="Read + save backup";erase.Text="Erase + blank check";program.Text="Program + verify";
        foreach(Button action in new[]{read,verify,blank,erase,program}){action.Dock=DockStyle.Fill;action.Margin=new Padding(0,4,10,6);actions.Controls.Add(action);}
        workspace.Controls.Remove(settings);workspace.ColumnStyles.Clear();workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));workspace.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,300));
        chipHelp.Dock=DockStyle.Fill;chipHelp.Margin=new Padding(16,0,0,0);workspace.Controls.Add(chipHelp,1,0);
        root.Controls.Add(header,0,0);root.Controls.Add(connection,0,1);root.Controls.Add(filePanel,0,2);root.Controls.Add(setup,0,3);root.Controls.Add(actions,0,4);root.Controls.Add(workspace,0,5);root.Controls.Add(log,0,6);
        root.ResumeLayout(true);
        FormClosing+=(s,e)=>{if(hardwareBusy){e.Cancel=true;MessageBox.Show("Wait for the current chip operation to finish before closing.");}};
        // OneROM-inspired desktop theme; programming routines are unchanged.
        root.Padding=new Padding(22,14,22,16);
        header.Controls.Clear();
        var brandMark=new ChipGlyph{Location=new Point(0,4),Size=new Size(44,44),BackColor=bg};header.Controls.Add(brandMark);
        header.Controls.Add(Label("BURN / DEVICE STUDIO",58,0,540,33,22,Color.FromArgb(242,243,237)));
        header.Controls.Add(Label("MOATES BURN1     /     MEMORY PROGRAMMING WORKSPACE",60,35,650,18,9,muted));
        var edition=Label("DESKTOP  /  v21",0,12,175,28,10,accent);edition.TextAlign=ContentAlignment.MiddleRight;edition.Anchor=AnchorStyles.Top|AnchorStyles.Right;header.Controls.Add(edition);header.Resize+=(s,e)=>edition.Left=header.ClientSize.Width-edition.Width-4;edition.Left=header.ClientSize.Width-edition.Width-4;edition.Top=0;var testRecord=Button("Test record",150);testRecord.Height=25;testRecord.Top=30;testRecord.Anchor=AnchorStyles.Top|AnchorStyles.Right;testRecord.Click+=(s,e)=>ShowTestRecord();header.Controls.Add(testRecord);header.Resize+=(s,e)=>testRecord.Left=header.ClientSize.Width-testRecord.Width-4;testRecord.Left=header.ClientSize.Width-testRecord.Width-4;
        connection.BackColor=panel;connection.Padding=new Padding(12,5,0,0);connection.Paint+=(s,e)=>{using(var pen=new Pen(Color.FromArgb(48,53,45)))e.Graphics.DrawRectangle(pen,0,0,connection.Width-1,connection.Height-1);};
        status.ForeColor=accent;status.Font=new Font("Consolas",10,FontStyle.Bold);
        ports.FlatStyle=FlatStyle.Flat;ports.BackColor=bg;ports.ForeColor=Color.FromArgb(231,234,226);ports.Font=new Font("Consolas",10);
        chips.FlatStyle=FlatStyle.Flat;chips.BackColor=bg;chips.ForeColor=Color.FromArgb(231,234,226);chips.Font=new Font("Segoe UI",10);
        StyleCombo(ports);StyleCombo(chips);
        offset.BackColor=bg;offset.ForeColor=accent;offset.Font=new Font("Consolas",11);offset.BorderStyle=BorderStyle.FixedSingle;
        filePanel.BackColor=panel;filePanel.Padding=new Padding(14,8,8,6);filePanel.Paint+=(s,e)=>{using(var pen=new Pen(Color.FromArgb(58,66,50)))e.Graphics.DrawRectangle(pen,0,0,filePanel.Width-1,filePanel.Height-1);};
        fileText.SetCellPosition(fileName,new TableLayoutPanelCellPosition(0,0));fileText.SetCellPosition(details,new TableLayoutPanelCellPosition(0,1));details.TextAlign=ContentAlignment.TopLeft;details.Margin=new Padding(3,0,3,0);fileName.Text="Choose a binary image";fileName.ForeColor=Color.FromArgb(242,243,237);fileName.Font=new Font("Segoe UI",15,FontStyle.Bold);
        details.Text="LOCAL BIN FILE   /   Load an image to inspect, compare or program";details.Font=new Font("Consolas",9);details.ForeColor=muted;
        open.Text="+  Open BIN";((StudioButton)open).Primary=true;
        read.Text="Read + backup";verify.Text="Verify vs BIN";blank.Text="Blank test";erase.Text="Erase + check";program.Text="Program + verify";((StudioButton)program).Primary=true;
        foreach(Button b in new[]{read,verify,blank,erase,program})b.Font=new Font("Segoe UI",10,FontStyle.Bold);
        var previewCard=new StudioCard{Dock=DockStyle.Fill,Margin=new Padding(0,0,12,0),BackColor=panel};
        var previewLayout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14,8,14,12),ColumnCount=1,RowCount=2};previewLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,30));previewLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var previewTitle=Label("IMAGE INSPECTOR  /  HEX + ASCII",0,0,500,24,10,muted);previewTitle.Font=new Font("Consolas",10);previewLayout.Controls.Add(previewTitle,0,0);previewLayout.Controls.Add(hex,0,1);previewCard.Controls.Add(previewLayout);
        hex.BackColor=panel;hex.ForeColor=Color.FromArgb(193,213,171);hex.Font=new Font("Consolas",10);hex.ScrollBars=ScrollBars.None;hex.Text="\r\n  NO IMAGE LOADED\r\n\r\n  Open a BIN to inspect its byte data.\r\n\r\n  Verify compares the chip against your loaded file.\r\n  Read + backup saves the contents of the selected chip.";
        var deviceCard=new StudioCard{Dock=DockStyle.Fill,BackColor=panel,Margin=new Padding(0)};
        var deviceLayout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(14,8,14,10),ColumnCount=1,RowCount=3};deviceLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,28));deviceLayout.RowStyles.Add(new RowStyle(SizeType.Percent,42));deviceLayout.RowStyles.Add(new RowStyle(SizeType.Percent,58));
        var deviceTitle=Label("CHIP PROFILE / TEST STATUS",0,0,270,24,9,muted);deviceTitle.Font=new Font("Consolas",9);deviceLayout.Controls.Add(deviceTitle,0,0);var glyph=new ChipGlyph{Dock=DockStyle.Fill,BackColor=panel,Large=true};deviceLayout.Controls.Add(glyph,0,1);chipHelp.Font=new Font("Segoe UI",9);chipHelp.ForeColor=Color.FromArgb(183,193,173);chipHelp.TextAlign=ContentAlignment.TopLeft;chipHelp.Margin=new Padding(0,8,0,0);deviceLayout.Controls.Add(chipHelp,0,2);deviceCard.Controls.Add(deviceLayout);
        workspace.Controls.Clear();workspace.Controls.Add(previewCard,0,0);workspace.Controls.Add(deviceCard,1,0);
        var logCard=new StudioCard{Dock=DockStyle.Fill,BackColor=panel,Margin=new Padding(0,10,0,0)};var logLayout=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(12,5,12,6),ColumnCount=1,RowCount=2};logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute,20));logLayout.RowStyles.Add(new RowStyle(SizeType.Percent,100));
        var logTitle=Label("ACTIVITY / DEVICE RESPONSES",0,0,650,20,9,muted);logTitle.Font=new Font("Consolas",9);logLayout.Controls.Add(logTitle,0,0);log.BackColor=panel;log.ForeColor=Color.FromArgb(164,180,149);logLayout.Controls.Add(log,0,1);logCard.Controls.Add(logLayout);root.Controls.Add(logCard,0,6);
        root.RowStyles[6].Height=105;
    }
    void ShowTestRecord(){using(var f=new Form{Text="BURN / Test record",Size=new Size(820,590),StartPosition=FormStartPosition.CenterParent,BackColor=bg,ForeColor=Color.White}){string record="Test record unavailable.";using(var stream=typeof(BurnModern).Assembly.GetManifestResourceStream("BurnModern.TestRecord")){if(stream!=null)using(var reader=new StreamReader(stream))record=reader.ReadToEnd();}var text=new TextBox{Dock=DockStyle.Fill,Multiline=true,ReadOnly=true,ScrollBars=ScrollBars.Vertical,BackColor=bg,ForeColor=Color.FromArgb(194,243,107),Font=new Font("Consolas",10),Text=record};f.Controls.Add(text);f.ShowDialog(this);}}
    Label Label(string text,int x,int y,int w,int h,int size,Color color){return new Label{Text=text,Location=new Point(x,y),Size=new Size(w,h),Font=new Font("Segoe UI",size),ForeColor=color,TextAlign=ContentAlignment.MiddleLeft};}
    void StyleCombo(ComboBox combo){combo.DrawMode=DrawMode.OwnerDrawFixed;combo.ItemHeight=23;combo.DrawItem+=(s,e)=>{Color fill=(e.State&DrawItemState.Selected)!=0?Color.FromArgb(42,50,32):bg;using(var brush=new SolidBrush(fill))e.Graphics.FillRectangle(brush,e.Bounds);if(e.Index>=0)TextRenderer.DrawText(e.Graphics,combo.Items[e.Index].ToString(),combo.Font,new Rectangle(e.Bounds.X+6,e.Bounds.Y,e.Bounds.Width-9,e.Bounds.Height),Color.FromArgb(228,237,214),TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);};}
    Button Button(string text,int width){return new StudioButton{Text=text,Width=width,Height=34,FlatStyle=FlatStyle.Flat,BackColor=panel,ForeColor=Color.White,Margin=new Padding(0,4,10,6),Cursor=Cursors.Hand};}
    bool hardwareBusy;
    internal static bool CheckBlank(byte[] bytes,out int first,out int count){if(bytes==null||bytes.Length==0)throw new ArgumentException("No chip bytes read.");first=-1;count=0;for(int i=0;i<bytes.Length;i++)if(bytes[i]!=255){if(first<0)first=i;count++;}return count==0;}
    internal static char ReadMode(int profile){return profile==8?'3':profile==10?'J':profile==1?'2':'5';}
    internal static int ProfileSize(int index){return index==8?4096:(index==9||index==11)?131072:index==10?262144:index==5?16384:(index==1||index==6)?32768:(index==2||index==4)?524288:65536;}
    int Capacity(){return ProfileSize(chips.SelectedIndex);}
    void RestoreLastProfile(){
        int saved=ProfilePreferences.Load(preferencePath);
        if(saved<0){selectionNotice.Text="No saved chip — confirm profile";selectionNotice.ForeColor=Color.FromArgb(225,189,115);Log("No valid saved chip profile. Current selection is a default; confirm the actual chip.");return;}
        restoringProfile=true;try{chips.SelectedIndex=saved;}finally{restoringProfile=false;}
        selectionNotice.Text="Remembered selection — confirm chip";selectionNotice.ForeColor=Color.FromArgb(225,189,115);UpdateChipControls();Log("Remembered profile: "+ProfilePreferences.Keys[saved]+". Confirm it matches the inserted chip; this is not chip-ID detection.");
    }
    internal static bool CanErase(int index){return index==0||index==2||index==3||index==4||index==10;}
    internal static bool CanProgram(int index){return index==0||index==1||index==2||index==4||index==10;}
    internal static string ProfileStatus(int index){
        if(index==11)return "EXPERIMENTAL READ ONLY: SST39SF010A, 128 KB. Same SST family as 39SF040, but BURN1 compatibility is not physically tested. Read, BIN verify and blank test only; erase/program disabled.";if(index==8)return "DOCUMENTED READ ONLY: 2732A / 27C32, 4 KB. Hardware test pending. External UV erase for windowed parts; no programming. Confirm exact chip type and socket placement.";if(index==9)return "EXPERIMENTAL READ ONLY: AM29F010, 128 KB, using AM29F040 read protocol. Hardware untested. Different sector layout: erase and program disabled.";if(index==10)return "ORIGINAL Ford F3 only, F2A adapter required. 5.14.F-compatible documented protocol; hardware untested. One externally selected 256 KB slot at a time. Full 256 KB raw BIN programming only. Not F3v2/v3. Remove module from ECU before use.";if(index==4)return "HISTORICAL PASS on your 512 KB test chip: read, erase, program and verify. Latest bench retry: full 512 KB program and double-read verify PASSED after reseat and USB power cycle. Earlier first-write rejection remains unexplained. Exact chip identity is unconfirmed. Other physical chips remain unvalidated.";
        if(index==3)return "HISTORICAL W27C512 read result recorded; no OneROM test reported. Two matching 64 KB reads with valid checksums. Experimental erase FAILED; programming unsupported.";
        if(index==1)return "ONEROM EMULATION READ PASSED for AT29C256; physical chip untested: two matching 32 KB reads with valid checksums. Programming and write verification are still untested. Page rewriting does not need a separate erase.";
        if(index==5)return "ONEROM EMULATION READ PASSED for 27C128; physical chip untested: four matching 16 KB reads across two tests, with the same SHA-256. An earlier read mismatch was reported. Read only; erase/program unsupported.";
        if(index==6)return "ONEROM EMULATION READ PASSED for 27C256; physical chip untested: two matching 32 KB reads with valid checksums. Read only; erase/program unsupported. Test status uses your reported profile, not electronic chip identification.";
        if(index==7)return "ONEROM EMULATION READ PASSED for 27C512; physical chip untested: four matching 64 KB reads across two tests, with the same SHA-256. Read only; erase/program unsupported. Test status uses your reported profile.";
        return "HARDWARE TESTING PENDING — Read, erase/blank check and program/verify are implemented. Programming requires a blank chip. No physical-chip test has been reported for this profile.";
    }
    void UpdateChipControls(){bool ready=!hardwareBusy&&connectedPort!=null;if(eraseControl!=null)eraseControl.Enabled=ready&&CanErase(chips.SelectedIndex);if(programControl!=null)programControl.Enabled=ready&&CanProgram(chips.SelectedIndex);if(verifyControl!=null)verifyControl.Enabled=ready;if(blankControl!=null)blankControl.Enabled=ready;if(readControl!=null)readControl.Enabled=ready;if(disconnectControl!=null)disconnectControl.Enabled=ready;if(chipHelp!=null)chipHelp.Text=ProfileStatus(chips.SelectedIndex);}
    bool EnsureConnection(){if(connectedPort!=null&&ports.SelectedItem as string==connectedPort){if(chips.SelectedIndex==10)return MessageBox.Show("Confirm ORIGINAL F3 (not v2/v3) connected through F2A, removed from ECU. Set its physical switch/jumper to the slot to operate. Only that 256 KB slot is accessed. Hardware testing is pending.","Original Ford F3 adapter required",MessageBoxButtons.YesNo,MessageBoxIcon.Warning,MessageBoxDefaultButton.Button2)==DialogResult.Yes;return true;}Log("Connect to the selected BURN1 port before starting a chip operation.");return false;}
    internal bool DisconnectSession(){if(hardwareBusy)return false;connectedPort=null;status.Text="●  OFFLINE";UpdateChipControls();Log("Disconnected. Serial handles are already released between operations. USB and chip-socket power are not switched off. Click Connect to resume.");return true;}
    internal sealed class Comparison {
        public int Differences,FirstAddress=-1,Count,Start; public byte Expected,Actual;
        public string Describe(){return Differences==0?"MATCH: all "+Count.ToString("N0")+" BIN bytes match chip addresses 0x"+Start.ToString("X6")+"–0x"+(Start+Count-1).ToString("X6")+". Bytes outside that range were not compared. Two chip reads match with valid checksums. No chip contents changed.":"MISMATCH: "+Differences.ToString("N0")+" of "+Count.ToString("N0")+" BIN bytes differ. First difference at chip address 0x"+FirstAddress.ToString("X6")+" (file offset 0x"+(FirstAddress-Start).ToString("X6")+"): file "+Expected.ToString("X2")+", chip "+Actual.ToString("X2")+". Bytes outside the file range were not compared. No chip contents changed.";}
    }
    internal static Comparison CompareFile(byte[] chipBytes,byte[] fileBytes,int start){
        if(chipBytes==null||fileBytes==null||fileBytes.Length==0||start<0||(long)start+fileBytes.Length>chipBytes.Length)throw new ArgumentException("File does not fit within the chip at this address.");
        var result=new Comparison{Count=fileBytes.Length,Start=start};for(int i=0;i<fileBytes.Length;i++)if(chipBytes[start+i]!=fileBytes[i]){result.Differences++;if(result.FirstAddress<0){result.FirstAddress=start+i;result.Expected=fileBytes[i];result.Actual=chipBytes[start+i];}}return result;
    }
    void RefreshPorts(){ports.Items.Clear();try{ports.Items.AddRange(SerialPort.GetPortNames());if(ports.Items.Count>0)ports.SelectedIndex=0;Log(ports.Items.Count+" serial port(s) found. No device commands sent.");}catch(Exception e){Log(e.Message);}}
    void Log(string text){log.AppendText(DateTime.Now.ToString("HH:mm:ss")+"  "+text+Environment.NewLine);}
    static string Identify(string name){
        foreach(int baud in new[]{115200,921600}){
            using(var port=new SerialPort(name,baud,Parity.None,8,StopBits.One)){
                port.ReadTimeout=1200;port.WriteTimeout=1200;
                port.Open();port.DiscardInBuffer();
                try{port.Write(new byte[]{0x56,0x56},0,2);int major=port.ReadByte(),minor=port.ReadByte(),hardware=port.ReadByte();if(major==5&&(hardware=='B'||hardware=='F'))return "BURN device responded on "+name+" at "+baud+" baud. Version bytes: "+major.ToString("X2")+" "+minor.ToString("X2")+" "+hardware.ToString("X2")+" ("+(char)hardware+"). Chip read/write support remains unverified.";return "Unrecognized version response: "+major.ToString("X2")+" "+minor.ToString("X2")+" "+hardware.ToString("X2")+". No chip operations attempted.";}
                catch(TimeoutException){ }
            }
        }
        return "No version response on "+name+" at 115200 or 921600 baud. No chip operations attempted.";
    }
    static byte[] ReadExact(SerialPort port,int count){var bytes=new byte[count];int done=0;while(done<count){int n=port.Read(bytes,done,count-done);if(n<=0)throw new IOException("Incomplete device response.");done+=n;}return bytes;}
    static string ExperimentalErase(string name,bool sst=false,bool testedProfile=false){
        bool sent=false;
        foreach(int baud in new[]{115200,921600}){
            using(var port=new SerialPort(name,baud,Parity.None,8,StopBits.One)){
                port.ReadTimeout=1800;port.WriteTimeout=1800;port.Open();port.DiscardInBuffer();byte[] version;
                try{port.Write(new byte[]{0x56,0x56},0,2);version=ReadExact(port,3);}catch(TimeoutException){continue;}
                if(version[0]!=5||(version[2]!='B'&&version[2]!='F'))throw new IOException("Unexpected device identity; no erase command sent.");
                port.ReadTimeout=5000;
                for(int bank=0;bank<(sst?8:1);bank++){
                    byte[] command=EraseCommand(sst,bank);port.Write(command,0,command.Length);sent=true;
                    int reply=port.ReadByte();if(reply!=0x4F)throw new IOException("Erase command "+bank+" returned 0x"+reply.ToString("X2")+" instead of O. No automatic retry performed.");
                }
            }
            break;
        }
        if(!sent)throw new IOException("No BURN identity response; no erase command sent.");
        byte[] contents=ReadTest(name,sst);int nonblank=0,first=-1;
        for(int i=0;i<contents.Length;i++)if(contents[i]!=255){nonblank++;if(first<0)first=i;}
        return nonblank==0?BlankResult(testedProfile):"Blank check FAILED: "+nonblank+" bytes are not FF; first at 0x"+first.ToString("X4")+". Chip is not blank. Do not program. No automatic erase retry performed.";
    }
    internal static string BlankResult(bool sst){return sst?"Blank check passed: two matching full-chip reads contain FF at every address. Your previously tested 512 KB chip passed program-and-verify. That result applies only to that physical chip; this app cannot identify it or confirm it is still inserted. This blank check alone does not verify programming on another chip.":"Blank check passed: two matching reads contain FF at every address. This is consistent with a blank chip, but an all-FF read can also indicate a contact problem. Programming remains unverified.";}
    internal static byte[] EraseCommand(bool sst,int bank){
        if(bank<0||bank>(sst?7:0))throw new ArgumentOutOfRangeException("bank");
        return sst?new byte[]{0x34,0x45,(byte)bank,(byte)(0x79+bank)}:new byte[]{0x35,0x45,0x7A};
    }
    static SerialPort OpenBurn(string name){
        foreach(int baud in new[]{115200,921600}){
            var port=new SerialPort(name,baud,Parity.None,8,StopBits.One);port.ReadTimeout=1800;port.WriteTimeout=1800;
            try{port.Open();port.DiscardInBuffer();port.Write(new byte[]{0x56,0x56},0,2);byte[] version=ReadExact(port,3);if(version[0]!=5||(version[2]!='B'&&version[2]!='F'))throw new IOException("Unexpected programmer identity.");return port;}
            catch(TimeoutException){port.Dispose();}
            catch{port.Dispose();throw;}
        }
        throw new IOException("No BURN identification response. No programming attempted.");
    }
    internal static byte[] WriteCommand(int address,byte[] image,int start,int count){
        if(address<0||count<1||count>256||address+count>524288||start<0||start+count>image.Length)throw new ArgumentOutOfRangeException();
        byte[] cmd=new byte[7+count];cmd[0]=0x34;cmd[1]=0x57;cmd[2]=(byte)count;cmd[3]=(byte)(address>>16);cmd[4]=(byte)(address>>8);cmd[5]=(byte)address;
        Array.Copy(image,start,cmd,6,count);int sum=0;for(int i=0;i<cmd.Length-1;i++)sum+=cmd[i];cmd[cmd.Length-1]=(byte)sum;return cmd;
    }
    static string ProgramTest(string name,byte[] image,IProgress<string> progress){
        if(image==null||image.Length<1||image.Length>524288)throw new IOException("Invalid BIN size.");
        progress.Report("Checking all 512 KB are blank (two reads). No erase command will be sent.");
        byte[] before=ReadTest(name,true);for(int i=0;i<before.Length;i++)if(before[i]!=255)throw new IOException("Chip is not blank at 0x"+i.ToString("X6")+"; no program commands sent.");
        progress.Report("Blank check passed. Programming "+image.Length.ToString("N0")+" bytes at 0x000000.");
        using(var port=OpenBurn(name)){
            port.ReadTimeout=5000;
            for(int address=0;address<image.Length;address+=256){int count=Math.Min(256,image.Length-address);byte[] cmd=WriteCommand(address,image,address,count);port.Write(cmd,0,cmd.Length);int reply=port.ReadByte();if(reply!=0x4F)throw new IOException("Write rejected at 0x"+address.ToString("X6")+" (reply 0x"+reply.ToString("X2")+").");if(address%65536==0)progress.Report("Programming: "+Math.Min(address+count,image.Length).ToString("N0")+" / "+image.Length.ToString("N0")+" bytes acknowledged.");}
        }
        progress.Report("Writes acknowledged. Verifying full 512 KB with two reads.");
        byte[] actual=ReadTest(name,true);for(int i=0;i<actual.Length;i++){byte expected=i<image.Length?image[i]:(byte)255;if(actual[i]!=expected)throw new IOException("Verify mismatch at 0x"+i.ToString("X6")+": expected "+expected.ToString("X2")+", read "+actual[i].ToString("X2")+".");}
        return "VERIFIED: "+image.Length.ToString("N0")+" BIN bytes match at 0x000000. All remaining bytes through 0x07FFFF are FF. Two full readbacks match with valid checksums.";
    }
    internal static byte[] StandardWriteCommand(int profile,int address,byte[] bytes,int start,int count){
        if(profile<0||profile>2||bytes==null||count<1||count>256||address<0||(long)address+count>ProfileSize(profile)||start<0||(long)start+count>bytes.Length)throw new ArgumentOutOfRangeException();
        if(profile==1&&(address%64!=0||count%64!=0))throw new ArgumentException("AT29C256 writes must contain complete, aligned 64-byte pages.");
        if(profile==2)return WriteCommand(address,bytes,start,count);
        byte[] cmd=new byte[6+count];cmd[0]=(byte)(profile==1?'2':'5');cmd[1]=(byte)'W';cmd[2]=(byte)count;cmd[3]=(byte)(address>>8);cmd[4]=(byte)address;Array.Copy(bytes,start,cmd,5,count);int sum=0;for(int i=0;i<cmd.Length-1;i++)sum+=cmd[i];cmd[cmd.Length-1]=(byte)sum;return cmd;
    }
    internal static byte[] PrepareExpected(int profile,byte[] before,byte[] image,int start){
        if(profile<0||profile>2||before==null||before.Length!=ProfileSize(profile)||image==null||image.Length<1||start<0||(long)start+image.Length>before.Length)throw new ArgumentException("BIN or placement is invalid.");
        if(profile!=1)for(int i=0;i<before.Length;i++)if(before[i]!=255)throw new IOException("Chip is not blank at 0x"+i.ToString("X6")+"; no program command sent.");
        byte[] expected=(byte[])before.Clone();Array.Copy(image,0,expected,start,image.Length);return expected;
    }
    static string ProgramStandard(string name,int profile,byte[] image,int start,IProgress<string> progress){
        bool large=profile==2;int size=ProfileSize(profile);char mode=ReadMode(profile);
        progress.Report(profile==1?"Reading existing AT29C256 contents twice before page rewrites.":"Checking the whole chip is blank (two reads).");
        byte[] before=ReadTest(name,large,size,0,mode);byte[] expected=PrepareExpected(profile,before,image,start);
        int first=profile==1?(start/64)*64:start;int end=profile==1?((start+image.Length+63)/64)*64:start+image.Length;
        progress.Report("Programming at 0x"+start.ToString("X6")+". Hardware testing of this profile is pending.");
        using(var port=OpenBurn(name)){
            port.ReadTimeout=5000;
            for(int address=first;address<end;){int count=profile==1?64:Math.Min(256,end-address);byte[] cmd=StandardWriteCommand(profile,address,expected,address,count);port.Write(cmd,0,cmd.Length);int reply=port.ReadByte();if(reply!='O')throw new IOException("Write rejected at 0x"+address.ToString("X6")+" (reply 0x"+reply.ToString("X2")+").");address+=count;if(address%4096==0||address==end)progress.Report("Write acknowledgements: "+(address-first).ToString("N0")+" / "+(end-first).ToString("N0")+" bytes.");}
        }
        progress.Report("Verifying the entire chip with two complete reads.");byte[] actual=ReadTest(name,large,size,0,mode);
        for(int i=0;i<size;i++)if(actual[i]!=expected[i])throw new IOException("Verify mismatch at 0x"+i.ToString("X6")+": expected "+expected[i].ToString("X2")+", read "+actual[i].ToString("X2")+".");
        return "VERIFIED: "+image.Length.ToString("N0")+" BIN bytes at 0x"+start.ToString("X6")+". Full "+size.ToString("N0")+"-byte readbacks match expected contents, including bytes outside the BIN. This result does not automatically change the profile's saved test status.";
    }
    static byte[] ReadTest(string name,bool sst=false,int readSize=0,int readBase=0,char mode='5'){
        int size=readSize==0?(sst?524288:65536):readSize;
        if(size<=0||size%256!=0||readBase<0||readBase%256!=0||(long)readBase+size>(sst?524288:65536))throw new ArgumentOutOfRangeException("readSize");
        foreach(int baud in new[]{115200,921600}){
            using(var port=new SerialPort(name,baud,Parity.None,8,StopBits.One)){
                port.ReadTimeout=1800;port.WriteTimeout=1800;port.Open();port.DiscardInBuffer();
                byte[] version;
                try{port.Write(new byte[]{0x56,0x56},0,2);version=ReadExact(port,3);}catch(TimeoutException){continue;}
                if(version[0]!=5||(version[2]!='B'&&version[2]!='F'))throw new IOException("Unexpected programmer identity. Chip test stopped.");
                var reads=new byte[2][];
                for(int pass=0;pass<2;pass++){
                    reads[pass]=new byte[size];
                    for(int address=0;address<size;address+=256){
                        byte[] cmd=ReadCommand(readBase+address,sst,mode);
                        port.Write(cmd,0,cmd.Length);byte[] block=ReadExact(port,257);int check=0;for(int i=0;i<256;i++)check+=block[i];
                        if((byte)check!=block[256])throw new IOException("Read checksum mismatch at 0x"+address.ToString("X4")+" on pass "+(pass+1)+". Test stopped.");
                        Array.Copy(block,0,reads[pass],address,256);
                    }
                }
                for(int i=0;i<size;i++)if(reads[0][i]!=reads[1][i])throw new IOException("The reads differ at 0x"+i.ToString("X6")+". Check chip contact and retry.");
                return reads[0];
            }
        }
        throw new IOException("No BURN version response at either supported baud rate. No chip read attempted.");
    }
    internal static byte[] ReadCommand(int address,bool sst,char mode='5'){
        if(mode!='5'&&mode!='2'&&mode!='3'&&mode!='J')throw new ArgumentOutOfRangeException("mode");
        if(address<0||address>=(sst?524288:65536)||address%256!=0)throw new ArgumentOutOfRangeException("address");
        if(!sst&&mode=='2'&&address>=32768)throw new ArgumentOutOfRangeException("address");
        if(mode=='J'&&!sst)throw new ArgumentException("F3 needs three address bytes.");if(mode=='3'&&address>=4096)throw new ArgumentOutOfRangeException("address");if(mode=='J'&&address>=262144)throw new ArgumentOutOfRangeException("address");
        byte[] cmd=sst?new byte[]{(byte)(mode=='J'?'J':'4'),0x52,0,(byte)(address>>16),(byte)(address>>8),(byte)address,0}:new byte[]{(byte)mode,0x52,0,(byte)(address>>8),(byte)address,0};
        int sum=0;for(int i=0;i<cmd.Length-1;i++)sum+=cmd[i];cmd[cmd.Length-1]=(byte)sum;return cmd;
    }
    void ValidatePlacement(){if(data==null){Log("Open a BIN file first.");return;}long end=(long)offset.Value+data.Length;if(end>Capacity())Log("Does not fit: selected address range exceeds chip capacity.");else Log("Fits: 0x"+((int)offset.Value).ToString("X6")+"–0x"+(end-1).ToString("X6")+" ("+data.Length+" bytes). Chip has not been written.");}
    void OpenBin(){if(hardwareBusy)return;using(var dialog=new OpenFileDialog{Filter="Binary files (*.bin)|*.bin|All files (*.*)|*.*",Title="Open chip binary"}){if(dialog.ShowDialog()!=DialogResult.OK)return;try{var info=new FileInfo(dialog.FileName);if(info.Length==0||info.Length>524288){MessageBox.Show("Choose a non-empty binary up to 512 KB. Firmware packages (.MFW) are not chip binaries.");return;}if(info.Extension.Equals(".mfw",StringComparison.OrdinalIgnoreCase)){MessageBox.Show("MFW packages are not supported as chip binaries.");return;}var bytes=File.ReadAllBytes(dialog.FileName);data=bytes;loadedFile=info.Name;fileName.Text=info.Name;string hash;using(var sha=SHA256.Create())hash=BitConverter.ToString(sha.ComputeHash(data)).Replace("-","");details.Text=data.Length.ToString("N0")+" bytes  •  SHA-256 "+hash.Substring(0,24)+"…";var sb=new System.Text.StringBuilder("  ADDRESS   HEX BYTES                                          ASCII\r\n\r\n");int limit=Math.Min(data.Length,4096);for(int i=0;i<limit;i+=16){sb.Append("  "+i.ToString("X6")+"    ");for(int j=0;j<16;j++)sb.Append(i+j<limit?data[i+j].ToString("X2")+" ":"   ");sb.Append("  ");for(int j=0;j<16&&i+j<limit;j++)sb.Append(data[i+j]>=32&&data[i+j]<127?(char)data[i+j]:'.');sb.AppendLine();}if(data.Length>limit)sb.AppendLine("  Preview limited to the first 4 KB.");hex.ScrollBars=ScrollBars.Both;hex.Text=sb.ToString();Log("Loaded "+info.Name+". SHA-256: "+hash);ValidatePlacement();}catch(Exception e){MessageBox.Show("Could not open file: "+e.Message);}}}
    [STAThread] static void Main(){Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new BurnModern());}
}

internal static class ProfilePreferences {
    internal static readonly string[] Keys={"sst27sf512","at29c256","am29f040","w27c512","sst39sf040-profile","27c128","27c256","27c512","2732a","am29f010-read","ford-f3-original","sst39sf010a-read"};
    internal static int Load(string path){try{if(!File.Exists(path))return -1;string key=File.ReadAllText(path).Trim();return Array.IndexOf(Keys,key);}catch(IOException){return -1;}catch(UnauthorizedAccessException){return -1;}}
    internal static void Save(string path,int index){if(index<0||index>=Keys.Length)throw new ArgumentOutOfRangeException("index");File.WriteAllText(path,Keys[index]+Environment.NewLine);}
}
static class StudioShapes {
    internal static GraphicsPath Round(RectangleF rect,float radius){float d=radius*2;var p=new GraphicsPath();p.AddArc(rect.X,rect.Y,d,d,180,90);p.AddArc(rect.Right-d,rect.Y,d,d,270,90);p.AddArc(rect.Right-d,rect.Bottom-d,d,d,0,90);p.AddArc(rect.X,rect.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
}
class StudioCard:Panel {
    public StudioCard(){DoubleBuffered=true;ResizeRedraw=true;}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);if(Width<20||Height<20)return;e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;using(var p=StudioShapes.Round(new RectangleF(0.5f,0.5f,Width-2,Height-2),9))using(var pen=new Pen(Color.FromArgb(48,53,45)))e.Graphics.DrawPath(pen,p);}
}
class StudioButton:Button {
    public bool Primary;bool hover,pressed;
    public StudioButton(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer,true);FlatAppearance.BorderSize=0;}
    protected override void OnMouseEnter(EventArgs e){hover=true;Invalidate();base.OnMouseEnter(e);}
    protected override void OnMouseLeave(EventArgs e){hover=false;pressed=false;Invalidate();base.OnMouseLeave(e);}
    protected override void OnMouseDown(MouseEventArgs e){pressed=true;Invalidate();base.OnMouseDown(e);}
    protected override void OnMouseUp(MouseEventArgs e){pressed=false;Invalidate();base.OnMouseUp(e);}
    protected override void OnPaint(PaintEventArgs e){
        e.Graphics.Clear(Parent==null?Color.FromArgb(17,19,16):Parent.BackColor);e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        Color fill=!Enabled?Color.FromArgb(27,30,25):Primary?(pressed?Color.FromArgb(154,198,80):hover?Color.FromArgb(208,255,128):Color.FromArgb(194,243,107)):(pressed?Color.FromArgb(43,49,37):hover?Color.FromArgb(46,53,40):Color.FromArgb(32,36,29));
        using(var path=StudioShapes.Round(new RectangleF(1,1,Width-3,Height-3),6))using(var brush=new SolidBrush(fill))using(var pen=new Pen(!Enabled?Color.FromArgb(39,43,35):Primary?fill:hover?Color.FromArgb(100,119,78):Color.FromArgb(62,71,52))){e.Graphics.FillPath(brush,path);e.Graphics.DrawPath(pen,path);}
        Color text=!Enabled?Color.FromArgb(90,98,82):Primary?Color.FromArgb(25,35,15):Color.FromArgb(230,235,222);TextRenderer.DrawText(e.Graphics,Text,Font,ClientRectangle,text,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis|TextFormatFlags.SingleLine);
        if(Focused&&ShowFocusCues)ControlPaint.DrawFocusRectangle(e.Graphics,new Rectangle(5,5,Width-10,Height-10),text,fill);
    }
}
class ChipGlyph:Control {
    public bool Large;
    public ChipGlyph(){DoubleBuffered=true;ResizeRedraw=true;}
    protected override void OnPaint(PaintEventArgs e){
        base.OnPaint(e);if(Width<20||Height<20)return;var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;Color lime=Color.FromArgb(194,243,107);float w=Large?Math.Min(122,Width-40):25,h=Large?Math.Min(50,Height-24):25;if(h<16)return;float x=(Width-w)/2,y=(Height-h)/2;
        using(var line=new Pen(Color.FromArgb(76,90,58),2)){
            if(Large){for(int i=0;i<10;i++){float px=x+7+i*(w-14)/9;g.DrawLine(line,px,y-6,px,y);g.DrawLine(line,px,y+h,px,y+h+6);}g.DrawLine(line,x-28,y+h/2,x,y+h/2);g.DrawLine(line,x+w,y+h/2,x+w+28,y+h/2);}
            else{for(int i=0;i<4;i++){float p=4+i*6;g.DrawLine(line,x+p,y-4,x+p,y);g.DrawLine(line,x+p,y+h,x+p,y+h+4);}}
        }
        using(var path=StudioShapes.Round(new RectangleF(x,y,w,h),Large?5:4))using(var fill=new SolidBrush(Large?Color.FromArgb(30,37,23):lime))using(var pen=new Pen(lime,1)){g.FillPath(fill,path);g.DrawPath(pen,path);}
        if(Large){using(var font=new Font("Consolas",12,FontStyle.Bold))TextRenderer.DrawText(g,"BURN1",font,new Rectangle((int)x,(int)y,(int)w,(int)h),lime,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);}
        else{using(var pen=new Pen(Color.FromArgb(30,41,19),2)){g.DrawLine(pen,x+8,y+7,x+8,y+18);g.DrawLine(pen,x+8,y+7,x+16,y+7);g.DrawLine(pen,x+8,y+12,x+15,y+12);g.DrawLine(pen,x+8,y+18,x+16,y+18);}}
    }
}
