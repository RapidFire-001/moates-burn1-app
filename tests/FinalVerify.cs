// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Rapidfire
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
class FinalVerify{
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Layout(Control c){c.PerformLayout();foreach(Control child in c.Controls)Layout(child);}
 [STAThread]static void Main(){
  byte[] chip={99,1,2,3,88},file={1,2,3};var result=BurnModern.CompareFile(chip,file,1);Check(result.Differences==0,"Offset match failed");Check(chip[0]==99&&chip[4]==88,"Comparison mutated chip buffer");
  chip[1]=8;chip[3]=9;result=BurnModern.CompareFile(chip,file,1);Check(result.Differences==2&&result.FirstAddress==1&&result.Expected==1&&result.Actual==8,"Mismatch reporting failed");
  var full=new byte[524288];var image=(byte[])full.Clone();full[full.Length-1]=255;result=BurnModern.CompareFile(full,image,0);Check(result.Differences==1&&result.FirstAddress==0x7FFFF,"Final byte missed");
  bool rejected=false;try{BurnModern.CompareFile(chip,file,3);}catch(ArgumentException){rejected=true;}Check(rejected,"Out of range accepted");
  using(var app=new BurnModern()){
   var flags=BindingFlags.NonPublic|BindingFlags.Instance;typeof(BurnModern).GetField("connectedPort",flags).SetValue(app,"TEST");typeof(BurnModern).GetMethod("UpdateChipControls",flags).Invoke(app,null);var combo=(ComboBox)typeof(BurnModern).GetField("chips",flags).GetValue(app);var verify=(Button)typeof(BurnModern).GetField("verifyControl",flags).GetValue(app);
   for(int i=0;i<12;i++){combo.SelectedIndex=i;Check(verify.Enabled,"Verify unavailable on profile "+i);}
   app.Size=new Size(1040,700);Layout(app);Control c=verify;while(c.Parent!=null){Check(c.Left>=0&&c.Top>=0&&c.Right<=c.Parent.ClientSize.Width&&c.Bottom<=c.Parent.ClientSize.Height,"Verify button clipped");c=c.Parent;}
  }
  Console.WriteLine("Passed: nonzero offset, ignored outside bytes, mismatch count/address/values, final 512 KB byte, range rejection, all profile verify controls, and minimum-size layout. No device commands sent.");
 }
}
