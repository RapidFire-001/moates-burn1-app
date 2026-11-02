// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Rapidfire
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

class RomStacker : Form {
    readonly List<byte[]> images=new List<byte[]>();
    readonly List<string> names=new List<string>();
    readonly ListBox rows=new ListBox{Dock=DockStyle.Fill,Font=new Font("Consolas",10)};
    readonly ComboBox banks=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Width=190};
    readonly Label summary=new Label{Dock=DockStyle.Fill,AutoSize=false};
    readonly Button save=new Button{Text="Save stacked BIN",AutoSize=true};
    public RomStacker(){
        Text="ROM stacker / BIN builder";Size=new Size(850,510);MinimumSize=new Size(740,460);StartPosition=FormStartPosition.CenterParent;
        BackColor=Color.FromArgb(25,28,24);ForeColor=Color.White;Font=new Font("Segoe UI",10);
        var root=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(16),RowCount=5,ColumnCount=1};
        root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));root.RowStyles.Add(new RowStyle(SizeType.Percent,100));root.RowStyles.Add(new RowStyle(SizeType.Absolute,46));root.RowStyles.Add(new RowStyle(SizeType.Absolute,54));root.RowStyles.Add(new RowStyle(SizeType.Absolute,48));Controls.Add(root);
        root.Controls.Add(new Label{Dock=DockStyle.Fill,Text="STACK ROM IMAGES IN ORDER\r\nFirst file starts at address zero. Sources are kept unchanged."});
        rows.BackColor=Color.FromArgb(17,19,16);rows.ForeColor=Color.FromArgb(194,243,107);root.Controls.Add(rows);
        var actions=new FlowLayoutPanel{Dock=DockStyle.Fill};root.Controls.Add(actions);
        AddButton(actions,"Add BINs",()=>AddFiles());AddButton(actions,"Move up",()=>MoveRow(-1));AddButton(actions,"Move down",()=>MoveRow(1));
        AddButton(actions,"Repeat",()=>{int i=rows.SelectedIndex;if(i<0)return;images.Insert(i+1,(byte[])images[i].Clone());names.Insert(i+1,names[i]);RefreshRows(i+1);});
        AddButton(actions,"Remove",()=>{int i=rows.SelectedIndex;if(i<0)return;images.RemoveAt(i);names.RemoveAt(i);RefreshRows(Math.Min(i,images.Count-1));});
        var options=new FlowLayoutPanel{Dock=DockStyle.Fill};root.Controls.Add(options);
        options.Controls.Add(new Label{Text="Bank size",AutoSize=true,Padding=new Padding(0,6,0,0)});
        banks.Items.AddRange(new object[]{"Join exact sizes","8 KB banks","16 KB banks","32 KB banks","64 KB banks","128 KB banks","256 KB banks","512 KB banks"});banks.SelectedIndex=0;options.Controls.Add(banks);
        options.Controls.Add(new Label{Text="Fixed banks pad each ROM at its end with FF.",AutoSize=true,Padding=new Padding(4,6,0,0)});
        var footer=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2};footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));footer.Controls.Add(summary);footer.Controls.Add(save);root.Controls.Add(footer);
        banks.SelectedIndexChanged+=(s,e)=>RefreshRows(rows.SelectedIndex);save.Click+=(s,e)=>Save();RefreshRows(-1);
    }
    void AddButton(FlowLayoutPanel panel,string text,Action action){var b=new Button{Text=text,AutoSize=true,BackColor=Color.FromArgb(42,50,32),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};b.Click+=(s,e)=>action();panel.Controls.Add(b);}
    int BankSize(){return banks.SelectedIndex==0?0:8192<<(banks.SelectedIndex-1);}
    void AddFiles(){using(var d=new OpenFileDialog{Title="Add ROM binaries",Filter="BIN files (*.bin)|*.bin",Multiselect=true}){if(d.ShowDialog(this)!=DialogResult.OK)return;try{var added=new List<byte[]>();foreach(string path in d.FileNames){var f=new FileInfo(path);if(f.Length<1||f.Length>524288)throw new IOException("Each ROM must contain 1 to 524,288 bytes.");added.Add(File.ReadAllBytes(path));}images.AddRange(added);foreach(string path in d.FileNames)names.Add(Path.GetFileName(path));RefreshRows(images.Count-1);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Cannot add ROM");}}}
    void MoveRow(int delta){int i=rows.SelectedIndex,j=i+delta;if(i<0||j<0||j>=images.Count)return;var b=images[i];images[i]=images[j];images[j]=b;string n=names[i];names[i]=names[j];names[j]=n;RefreshRows(j);}
    void RefreshRows(int selected){rows.Items.Clear();long start=0;int bank=BankSize();bool fits=true;for(int i=0;i<images.Count;i++){rows.Items.Add(String.Format("{0,2}  0x{1:X6}  {2,7:N0} bytes  {3}",i+1,start,images[i].Length,names[i]));if(bank>0&&images[i].Length>bank)fits=false;start+=bank>0?bank:images[i].Length;}if(selected>=0&&selected<rows.Items.Count)rows.SelectedIndex=selected;save.Enabled=images.Count>0&&fits&&start<=524288;summary.Text=!fits?"A ROM exceeds the selected bank size.":start>524288?"Stack exceeds this app's 512 KB limit.":String.Format("{0} ROM(s) / {1:N0} bytes. Maximum 512 KB.",images.Count,start);}
    internal static byte[] Build(IList<byte[]> roms,int bank){
        if(roms==null||roms.Count==0||bank<0)throw new ArgumentException("Add at least one ROM and select a valid bank size.");
        long total=0;foreach(var rom in roms){if(rom==null||rom.Length==0||(bank>0&&rom.Length>bank))throw new ArgumentException("ROM is empty or exceeds its bank.");total+=bank>0?bank:rom.Length;}
        if(total>524288)throw new ArgumentException("Stack exceeds 512 KB.");var output=new byte[(int)total];for(int i=0;i<output.Length;i++)output[i]=255;int pos=0;foreach(var rom in roms){Buffer.BlockCopy(rom,0,output,pos,rom.Length);pos+=bank>0?bank:rom.Length;}return output;
    }
    void Save(){try{byte[] output=Build(images,BankSize());using(var d=new SaveFileDialog{Title="Save new stacked binary",Filter="BIN files (*.bin)|*.bin",DefaultExt="bin",FileName="stacked-roms.bin",OverwritePrompt=true}){if(d.ShowDialog(this)!=DialogResult.OK)return;string target=Path.GetFullPath(d.FileName);if(File.Exists(target))throw new IOException("Choose a new filename; the stacker does not overwrite existing files.");using(var stream=new FileStream(target,FileMode.CreateNew,FileAccess.Write)){stream.Write(output,0,output.Length);}MessageBox.Show(this,"Saved "+output.Length.ToString("N0")+" bytes. Open the new BIN in the main window to check chip placement. No chip commands were sent.","Stack saved");}}catch(Exception ex){MessageBox.Show(this,ex.Message,"Stack not saved");}}
}
