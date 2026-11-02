// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Rapidfire
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
class FinalWorkflow {
 static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
 static void Layout(Control c){c.PerformLayout();foreach(Control child in c.Controls)Layout(child);}
 [STAThread]static void Main(){
   foreach(int p in new[]{0,1,2}){
     var before=new byte[BurnModern.ProfileSize(p)];for(int i=0;i<before.Length;i++)before[i]=p==1?(byte)(i%251):(byte)255;
     var image=new byte[]{18,52,86};byte[] expected=BurnModern.PrepareExpected(p,before,image,63);
     Check(expected[62]==before[62]&&expected[63]==18&&expected[65]==86&&expected[66]==before[66],"Placement/preservation failure");
     if(p!=1){before[0]=0;bool rejected=false;try{BurnModern.PrepareExpected(p,before,image,63);}catch(System.IO.IOException){rejected=true;}Check(rejected,"Nonblank flash accepted");}
   }
   byte[] page=new byte[64];page[0]=0x12;var cmd=BurnModern.StandardWriteCommand(1,0x7FC0,page,0,64);
   Check(cmd.Length==70&&cmd[0]=='2'&&cmd[2]==64&&cmd[3]==0x7F&&cmd[4]==0xC0,"AT29 page packet failure");
   bool bad=false;try{BurnModern.StandardWriteCommand(1,1,page,0,64);}catch(ArgumentException){bad=true;}Check(bad,"Unaligned AT29 page accepted");
   foreach(int p in new[]{0,2}){byte[] block=new byte[256];cmd=BurnModern.StandardWriteCommand(p,BurnModern.ProfileSize(p)-256,block,0,256);Check(cmd[2]==0&&cmd[0]==(p==0?'5':'4'),"Final flash block packet failure");}
   using(var app=new BurnModern()){
     var flags=BindingFlags.NonPublic|BindingFlags.Instance;typeof(BurnModern).GetField("connectedPort",flags).SetValue(app,"TEST");typeof(BurnModern).GetMethod("UpdateChipControls",flags).Invoke(app,null);var t=typeof(BurnModern);var chips=(ComboBox)t.GetField("chips",flags).GetValue(app);var erase=(Button)t.GetField("eraseControl",flags).GetValue(app);var program=(Button)t.GetField("programControl",flags).GetValue(app);
     for(int p=0;p<12;p++){chips.SelectedIndex=p;Check(erase.Enabled==BurnModern.CanErase(p)&&program.Enabled==BurnModern.CanProgram(p),"Profile controls mismatch");Check(!String.IsNullOrEmpty(BurnModern.ProfileStatus(p)),"Missing status");}
     foreach(Size size in new[]{new Size(1040,700),new Size(1120,790)}){
       app.Size=size;Layout(app);
       foreach(Button b in new[]{erase,program}){Control c=b;while(c.Parent!=null){Check(c.Left>=0&&c.Top>=0&&c.Right<=c.Parent.ClientSize.Width&&c.Bottom<=c.Parent.ClientSize.Height,"Clipped action at "+size+": "+b.Text);c=c.Parent;}}
     }
   }
   Console.WriteLine("Passed: all profile capabilities, minimum-size action layout, flash blank requirement, nonzero placement, AT29 page alignment and preservation, and final-address write packets. No hardware commands sent.");
 }
}
