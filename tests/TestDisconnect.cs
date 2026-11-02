// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Rapidfire
using System;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
class TestDisconnect {
 static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
 static void Layout(Control c){c.PerformLayout();foreach(Control child in c.Controls)Layout(child);}
 [STAThread]static void Main(){using(var app=new BurnModern()){
  var t=typeof(BurnModern);var flags=BindingFlags.NonPublic|BindingFlags.Instance;var connected=t.GetField("connectedPort",flags);var busy=t.GetField("hardwareBusy",flags);var update=t.GetMethod("UpdateChipControls",flags);var chips=(ComboBox)t.GetField("chips",flags).GetValue(app);int selected=chips.SelectedIndex;
  foreach(string name in new[]{"readControl","verifyControl","eraseControl","programControl","disconnectControl"})Check(!((Button)t.GetField(name,flags).GetValue(app)).Enabled,"Offline control enabled: "+name);
  connected.SetValue(app,"TEST");update.Invoke(app,null);Check(((Button)t.GetField("disconnectControl",flags).GetValue(app)).Enabled,"Disconnect not enabled");
  busy.SetValue(app,true);update.Invoke(app,null);Check(!app.DisconnectSession(),"Disconnected during an operation");Check((string)connected.GetValue(app)=="TEST","Busy disconnect cleared session");Check(!((Button)t.GetField("disconnectControl",flags).GetValue(app)).Enabled,"Disconnect enabled while busy");
  busy.SetValue(app,false);Check(app.DisconnectSession(),"Idle disconnect failed");Check(connected.GetValue(app)==null,"Session not cleared");Check(chips.SelectedIndex==selected,"Disconnect changed remembered profile");
  foreach(string name in new[]{"readControl","verifyControl","eraseControl","programControl","disconnectControl"})Check(!((Button)t.GetField(name,flags).GetValue(app)).Enabled,"Disconnected control enabled: "+name);
  app.Size=new Size(1040,700);Layout(app);Control c=(Button)t.GetField("disconnectControl",flags).GetValue(app);while(c.Parent!=null){Check(c.Left>=0&&c.Top>=0&&c.Right<=c.Parent.ClientSize.Width&&c.Bottom<=c.Parent.ClientSize.Height,"Disconnect clipped");c=c.Parent;}
  Console.WriteLine("Passed: offline controls, connected state, busy disconnect blocked, idle disconnect clears session, profile retained, minimum-width layout. No device commands sent.");
 }}
}
