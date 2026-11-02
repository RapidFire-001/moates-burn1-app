// SPDX-License-Identifier: MIT
// Copyright (c) 2026 Rapidfire
using System;using System.Drawing;using System.Drawing.Imaging;using System.Windows.Forms;
class TestStacker{
static void Check(bool x){if(!x)throw new Exception("Stack failed");}
[STAThread]static void Main(){
var a=new byte[]{1,2};var b=new byte[]{3};var r=RomStacker.Build(new[]{a,b,a},0);Check(BitConverter.ToString(r)=="01-02-03-01-02");
r=RomStacker.Build(new[]{a,b},4);Check(BitConverter.ToString(r)=="01-02-FF-FF-03-FF-FF-FF");Check(a[0]==1&&b[0]==3);
var full=new byte[131072];r=RomStacker.Build(new[]{full,full,full,full},131072);Check(r.Length==524288);
foreach(var pair in new[]{new[]{new byte[0]},new[]{new byte[524288],new byte[1]}}){bool bad=false;try{RomStacker.Build(pair,0);}catch(ArgumentException){bad=true;}Check(bad);}
bool tooBig=false;try{RomStacker.Build(new[]{a},1);}catch(ArgumentException){tooBig=true;}Check(tooBig);
Application.EnableVisualStyles();using(var f=new RomStacker()){f.Location=new Point(-30000,-30000);f.StartPosition=FormStartPosition.Manual;f.ShowInTaskbar=false;var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;var imgs=(System.Collections.Generic.List<byte[]>)typeof(RomStacker).GetField("images",flags).GetValue(f);var labels=(System.Collections.Generic.List<string>)typeof(RomStacker).GetField("names",flags).GetValue(f);for(int i=0;i<4;i++){imgs.Add(new byte[131072]);labels.Add("Example-ROM-"+(i+1)+".bin");}((ComboBox)typeof(RomStacker).GetField("banks",flags).GetValue(f)).SelectedIndex=5;f.Show();Application.DoEvents();using(var bmp=new Bitmap(f.Width,f.Height)){f.DrawToBitmap(bmp,new Rectangle(Point.Empty,f.Size));bmp.Save("artifacts/rom-stacker-preview.png",ImageFormat.Png);}f.Close();}
Console.WriteLine("PASS: ordering, repeated images, FF bank padding, source preservation, full 512 KB stack, and invalid/oversized rejection.");}}
