[CmdletBinding()]
param([string]$ReferenceSave,[string]$PlayerPath,[switch]$CommandUiOnly,[ValidateRange(1024,3840)][int]$Width=1440,[ValidateRange(720,2160)][int]$Height=900)
$ErrorActionPreference='Stop'
$projectRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if(-not $PlayerPath){$PlayerPath=Join-Path $projectRoot 'Builds\Windows\PlanetOne.exe'}
$PlayerPath=[IO.Path]::GetFullPath($PlayerPath)
if(-not (Test-Path -LiteralPath $PlayerPath)){throw 'Windows-Player fehlt.'}
$outputDirectory=Join-Path $projectRoot ('TestResults\Presentation\'+(Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
# A separate native desktop, never switched to or activated. No global input injection.
Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;
public static class PlanetOneIsolatedDesktop {
 [StructLayout(LayoutKind.Sequential,CharSet=CharSet.Unicode)] struct Startup {
  public int cb;public string reserved,desktop,title;public int x,y,width,height,xChars,yChars,fill,flags;public short show,reservedSize;public IntPtr reservedPtr,input,output,error;
 }
 [StructLayout(LayoutKind.Sequential)] struct ProcessInfo {public IntPtr process,thread;public uint processId,threadId;}
 [DllImport("user32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern IntPtr CreateDesktop(string name,IntPtr device,IntPtr mode,uint flags,uint access,IntPtr security);
 [DllImport("user32.dll")] static extern bool CloseDesktop(IntPtr desktop);
 [DllImport("kernel32.dll",CharSet=CharSet.Unicode,SetLastError=true)] static extern bool CreateProcess(string application,StringBuilder command,IntPtr processAttributes,IntPtr threadAttributes,bool inherit,uint flags,IntPtr environment,string directory,ref Startup startup,out ProcessInfo process);
 [DllImport("kernel32.dll")] static extern uint WaitForSingleObject(IntPtr handle,uint milliseconds);
 [DllImport("kernel32.dll")] static extern bool GetExitCodeProcess(IntPtr process,out uint code);
 [DllImport("kernel32.dll")] static extern bool TerminateProcess(IntPtr process,uint code);
 [DllImport("kernel32.dll")] static extern bool CloseHandle(IntPtr handle);
 static IntPtr desktopHandle;static ProcessInfo child;
 public static void Start(string executable,string arguments,string directory){
  string name="PlanetOneTest_"+Guid.NewGuid().ToString("N");
  desktopHandle=CreateDesktop(name,IntPtr.Zero,IntPtr.Zero,0,0x01ff,IntPtr.Zero);
  if(desktopHandle==IntPtr.Zero)throw new Win32Exception(Marshal.GetLastWin32Error());
  var startup=new Startup{cb=Marshal.SizeOf(typeof(Startup)),desktop="winsta0\\"+name};
  if(!CreateProcess(executable,new StringBuilder("\""+executable+"\" "+arguments),IntPtr.Zero,IntPtr.Zero,false,0,IntPtr.Zero,directory,ref startup,out child)){
   int error=Marshal.GetLastWin32Error();CloseDesktop(desktopHandle);desktopHandle=IntPtr.Zero;throw new Win32Exception(error);
  }
 }
 public static bool Wait(){return WaitForSingleObject(child.process,10000)==0;}
 public static uint ExitCode(){uint code;GetExitCodeProcess(child.process,out code);return code;}
 public static void Close(){
  if(child.process!=IntPtr.Zero){if(WaitForSingleObject(child.process,0)!=0)TerminateProcess(child.process,124);WaitForSingleObject(child.process,5000);CloseHandle(child.thread);CloseHandle(child.process);child=new ProcessInfo();}
  if(desktopHandle!=IntPtr.Zero){CloseDesktop(desktopHandle);desktopHandle=IntPtr.Zero;}
 }
}
'@
$arguments='-planetOneSmoke -screen-width '+$Width+' -screen-height '+$Height+' -screen-fullscreen 0 -force-d3d11 -smokeOutput "'+$outputDirectory+'" -logFile "'+(Join-Path $outputDirectory 'player.log')+'"'
if($ReferenceSave){$referencePath=(Resolve-Path -LiteralPath $ReferenceSave).Path;$arguments+=' -referenceSave "'+$referencePath+'"'}
if($CommandUiOnly){$arguments+=' -commandUiOnly'}
try {
 [PlanetOneIsolatedDesktop]::Start($PlayerPath,$arguments,$projectRoot)
 $finished=$false
 for($i=0;$i -lt 18;$i++){
  if([PlanetOneIsolatedDesktop]::Wait()){$finished=$true;break}
  Write-Output 'Isolierter GUI-Test laeuft; Arbeitsdesktop bleibt unveraendert.'
 }
 if(-not $finished){throw 'Isolierter GUI-Test nach 180 Sekunden beendet.'}
 if([PlanetOneIsolatedDesktop]::ExitCode() -ne 0){throw ('Player fehlgeschlagen: '+$outputDirectory)}
 $report=Join-Path $outputDirectory 'smoke-result.txt'
 if(-not (Test-Path -LiteralPath $report)){throw 'Player lieferte kein Pruefprotokoll.'}
 foreach($layout in @('CommandCenter','OrbitalBridge','OpenSpace')){
  $views=if($CommandUiOnly){@('building','unit')}else{@('surface','space')}
  foreach($view in $views){
   $imageName=if($CommandUiOnly){'commands-'+$view+'-'+$layout+'.png'}else{'layout-'+$layout+'-'+$view+'.png'}
   $imagePath=Join-Path $outputDirectory $imageName
   if(-not (Test-Path -LiteralPath $imagePath) -or (Get-Item -LiteralPath $imagePath).Length -lt 1024){throw ('GUI-Bild fehlt: '+$imagePath)}
  }
 }
 if(Select-String -LiteralPath (Join-Path $outputDirectory 'player.log') -Pattern 'GUI Error|Exception:|Shader error' -Quiet){throw ('Fehler im Player-Log: '+$outputDirectory)}
 Get-Content -LiteralPath $report
 Write-Output ('Isolierte GUI-Bilder: '+$outputDirectory)
} finally {[PlanetOneIsolatedDesktop]::Close()}
