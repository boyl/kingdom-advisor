#Requires -Version 7.0
$ErrorActionPreference='Stop'
$source=@('Localization.cs','SeasonPresentation.cs','NativeSeasonReader.cs') | ForEach-Object {Get-Content (Join-Path $PSScriptRoot "src/$_") -Raw}
$body=($source | ForEach-Object {[regex]::Replace($_,'(?m)^using [^;]+;\r?\n','')}) -join "`n"
$body=$body.Replace('internal sealed class NativeSeasonReader','public sealed class NativeSeasonReader')
Add-Type -TypeDefinition (@'
using System;using System.Linq;using System.Collections.Generic;using System.Text.RegularExpressions;
public enum Season {Spring,Summer,Autumn,Winter}
public class Director {
 public int CurrentSeasonDay,Queries,Id=1;
 public int GetInstanceID()=>Id;
 public Season CurrentSeason=>At(CurrentSeasonDay);
 private Season At(int day)=>(Season)((day/16)%4);
 public Season GetNextDaySeason()=>At(CurrentSeasonDay+1);
 public Season GetSeasonFromDay(int day){Queries++;return At(day);}
}
'@ + $body + @'
public static class SeasonTests {
 public static string Run(){
 int checks=0;void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
 string[] names={"Spring","Summer","Autumn","Winter"};
 foreach(int length in new[]{7,16,20})foreach(int year in new[]{0,1,12})foreach(int season in new[]{0,1,2,3})foreach(int offset in new[]{0,length-1}){
  int day=(year*4+season)*length+offset;string At(int d)=>names[(d/length)%4];
  var result=KingdomAdvisor.SeasonPresentation.Forecast(At(day),day,At(day+1),At);
  Check(result.Days==length-offset&&result.Next==names[(season+1)%4],"Variable cycle / last day / year rollover");
 }
 Check(KingdomAdvisor.SeasonPresentation.Forecast("Winter",10,"Winter",d=>"Summer").Days==-1,"Reject mismatched date coordinate");
 Check(KingdomAdvisor.SeasonPresentation.Forecast("Winter",10,"Summer",d=>"Winter").Days==-1,"Reject mismatched tomorrow");
 int reads=0;Check(KingdomAdvisor.SeasonPresentation.Forecast("Winter",10,"Winter",d=>{reads++;return "Winter";}).Days==-1&&reads<=130,"Fixed season: bounded unknown");
 Check(KingdomAdvisor.SeasonPresentation.Forecast("Unknown",0,"Unknown",d=>"Unknown").Days==-1,"Unknown season");
 var director=new Director{CurrentSeasonDay=63};var reader=new KingdomAdvisor.NativeSeasonReader();
 Check(reader.Read(director,"save1").Days==1,"Native final winter day");int queried=director.Queries;
 reader.Read(director,"save1");Check(director.Queries==queried,"No repeated native forecast in the same day");
 director.CurrentSeasonDay++;Check(reader.Read(director,"save1").Days==16,"Refresh on date change");
 queried=director.Queries;reader.Read(director,"save2");Check(director.Queries>queried,"Refresh on save/context change");
 foreach(bool english in new[]{false,true}){
  KingdomAdvisor.Localization.English=english;
  foreach(var season in names){var hint=KingdomAdvisor.Localization.Text(KingdomAdvisor.SeasonPresentation.Hint(season));Check(hint.Length>0&&(!english||!Regex.IsMatch(hint,"[\u4e00-\u9fff]")),"Localized hint");}
  string header=KingdomAdvisor.SeasonPresentation.Header("Winter","Spring",8);
  Check(header==(english?"Winter · 8 days until Spring":"冬季 · 距春季 8 天"),"Localized countdown");
 }
 return "Season outlook: "+checks+" checks passed";
 }
}
'@)
[SeasonTests]::Run()
