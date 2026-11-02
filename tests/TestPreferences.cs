// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Rapidfire
using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
class TestPreferences {
 static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
 [STAThread]static void Main(){
  string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preferences-test-"+Guid.NewGuid().ToString("N")+".settings");
  try{
   Check(ProfilePreferences.Load(path)==-1,"Missing preference should not select a chip");
   for(int i=0;i<12;i++){ProfilePreferences.Save(path,i);Check(ProfilePreferences.Load(path)==i,"Profile persistence failure");Check(File.ReadAllText(path).Trim()==ProfilePreferences.Keys[i],"Saved display text instead of stable key");}
   File.WriteAllText(path,"not-a-chip");Check(ProfilePreferences.Load(path)==-1,"Unknown profile accepted");
   File.WriteAllText(path,"");Check(ProfilePreferences.Load(path)==-1,"Empty setting accepted");
  }finally{if(File.Exists(path))File.Delete(path);}
  string appPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"BurnModern.settings");byte[] original=File.Exists(appPath)?File.ReadAllBytes(appPath):null;
  try{
   var flags=BindingFlags.NonPublic|BindingFlags.Instance;
   using(var app=new BurnModern()){var chips=(ComboBox)typeof(BurnModern).GetField("chips",flags).GetValue(app);chips.SelectedIndex=1;}
   using(var app=new BurnModern()){var chips=(ComboBox)typeof(BurnModern).GetField("chips",flags).GetValue(app);var notice=(Label)typeof(BurnModern).GetField("selectionNotice",flags).GetValue(app);Check(chips.SelectedIndex==1,"Startup failed to restore AT29C256");Check(notice.Text.StartsWith("Remembered selection"),"Missing remembered label");chips.SelectedIndex=7;Check(ProfilePreferences.Load(appPath)==7,"Manual selection was not saved");Check(notice.Text=="Manual chip selection","Manual selection label incorrect");}
  }finally{if(original!=null)File.WriteAllBytes(appPath,original);else if(File.Exists(appPath))File.Delete(appPath);}
  Console.WriteLine("Passed all 12 profile keys, missing/invalid settings, restart restoration, manual selection persistence and honest selection labels. No serial commands sent.");
 }
}
