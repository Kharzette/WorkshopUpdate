using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;


namespace UpdateWorkshop
{
	class Program
	{
		static Dictionary<int, string>	sModPaths	=new Dictionary<int, string>();

		static void Main(string []args)
		{
			if(!File.Exists("Config.txt"))
			{
				Console.WriteLine("Config file Config.txt does not exist...");
				return;
			}
			Console.WriteLine("Config exists!");

			FileStream	fs	=new FileStream("Config.txt", FileMode.Open, FileAccess.Read);
			if(fs == null)
			{
				Console.WriteLine("Unable to open config file.");
				return;
			}

			StreamReader	sr	=new StreamReader(fs);
			if(sr == null)
			{
				Console.WriteLine("Unable to open config file.");
				return;
			}

			string		steamCmdPath	="";
			string		installDir		="";
			int			gameAppID		=0;
			int			serverAppID		=0;
			string		modListPath		="";
			List<int>	mods			=new List<int>();

			while(!sr.EndOfStream)
			{
				string	line	=sr.ReadLine();
				if(line == "")
				{
					//skip blank lines
					continue;
				}

				string	[]toks	=line.Split(' ', '\t');

				if(toks.Length < 2)
				{
					//bad line
					Console.WriteLine("Bad line in Config.txt at position: " + sr.BaseStream.Position);
					continue;
				}

				//skip whitespace
				int	idx	=0;
				while(idx < toks.Length)
				{
					if(toks[idx] == "" || toks[idx] == " " || toks[idx] == "\t")
					{
						idx++;
						continue;
					}
					break;
				}

				//skip commented lines
				if(toks[idx].StartsWith("//"))
				{
					continue;
				}

				string	cname	=toks[idx];
				idx++;

				while(idx < toks.Length)
				{
					if(toks[idx] == "" || toks[idx] == " " || toks[idx] == "\t")
					{
						idx++;
						continue;
					}
					break;
				}

				if(cname == "SteamCmdPath")
				{
					//grab whole rest of line as path, so spaces work etc
					int	pathStart	=line.IndexOf(toks[idx]);

					steamCmdPath	=line.Substring(pathStart);

					steamCmdPath	=steamCmdPath.TrimEnd(' ', '\t');
					steamCmdPath	+="/steamcmd.exe";

					continue;
				}
				else if(cname == "InstallDir")
				{
					//grab whole rest of line as path, so spaces work etc
					int	pathStart	=line.IndexOf(toks[idx]);

					installDir	=line.Substring(pathStart);

					installDir	=installDir.TrimEnd(' ', '\t');

					continue;
				}
				else if(cname == "GameAppID")
				{
					if(!int.TryParse(toks[idx], out gameAppID))
					{
						Console.WriteLine("Bad token looking for game app id.");
						continue;
					}
				}
				else if(cname == "ServerAppID")
				{
					if(!int.TryParse(toks[idx], out serverAppID))
					{
						Console.WriteLine("Bad token looking for server app id.");
						continue;
					}
				}
				else if(cname == "ModListPath")
				{
					//grab whole rest of line as path, so spaces work etc
					int	pathStart	=line.IndexOf(toks[idx]);

					modListPath	=line.Substring(pathStart);

					modListPath	=modListPath.TrimEnd(' ', '\t');

					continue;
				}
				else if(cname == "Mod")
				{
					int	modID	=0;
					if(!int.TryParse(toks[idx], out modID))
					{
						Console.WriteLine("Bad token looking for mod id.");
						continue;
					}

					if(mods.Contains(modID))
					{
						Console.WriteLine("Duplicate mod " + modID + " ignored.");
					}

					mods.Add(modID);
				}
			}

			sr.Close();
			fs.Close();

			fs	=null;
			sr	=null;

			Console.WriteLine("SteamCmd Path is: " + steamCmdPath);
			Console.WriteLine("InstallDir is: " + installDir);
			Console.WriteLine("Game AppID is: " + gameAppID);
			Console.WriteLine("Server AppID is: " + serverAppID);

			//Console.ReadKey();

			foreach(int mod in mods)
			{
				Process	scProc	=FireProcess(QS(steamCmdPath), "+login anonymous" +
					" +workshop_download_item " + gameAppID + " " + mod + " +quit");

				scProc.WaitForExit();

				scProc.Close();
			}

			if(modListPath != "")
			{
				EditModList(modListPath);
			}

			Console.WriteLine("Done!");
		}


		//for conan servers
		static void EditModList(string modListPath)
		{
			Console.WriteLine("Mod list path provided, will update...");

			string	txtFilePath	="";
			if(modListPath.EndsWith(".txt"))
			{
				//they point to the actual file
				txtFilePath	=modListPath;
			}
			else
			{
				//pointed at the folder
				txtFilePath	=modListPath + "\\" + "modlist.txt";
			}

			FileStream	fs	=null;

			if(File.Exists(txtFilePath))
			{
				Console.WriteLine("Mod list found, editing...");

				fs	=new FileStream(txtFilePath, FileMode.Open, FileAccess.Write);
			}
			else
			{
				Console.WriteLine("modlist.txt doesn't exist, making one...");

				if(!Directory.Exists(modListPath))
				{
					Console.WriteLine("Path: " + modListPath + " does not exist...");
				}
				else
				{
					fs	=new FileStream(txtFilePath, FileMode.CreateNew, FileAccess.Write);
				}
			}

			if(fs == null)
			{
				Console.WriteLine("Unable to open modlist.txt");
				return;
			}

			StreamWriter	sw	=new StreamWriter(fs);
			if(sw == null)
			{
				Console.WriteLine("Unable to open modlist.txt");
				return;
			}

			foreach(KeyValuePair<int, string> mod in sModPaths)
			{
				DirectoryInfo	di	=new DirectoryInfo(mod.Value);

				FileInfo	[]fi	=di.GetFiles("*.pak", SearchOption.TopDirectoryOnly);
				if(fi.Length == 0)
				{
					Console.WriteLine("Mod " + mod.Key + " has no pak file...");
					continue;
				}
				else if(fi.Length > 1)
				{
					Console.WriteLine("Mod " + mod.Key + " has more than one pak file...");
				}

				sw.WriteLine(mod.Value + "\\" + fi[0].Name);
			}

			sw.Close();
			fs.Close();
		}


		static void OnProcessSpew(object sender, EventArgs ea)
		{
			DataReceivedEventArgs	drea	=ea as DataReceivedEventArgs;
			if(drea == null || drea.Data == null)
			{
				return;
			}
			Console.WriteLine(drea.Data);

			if(drea.Data.StartsWith("Success. Downloaded item"))
			{
				string	modString	=drea.Data.Substring(25);

				int	endIdx	=modString.IndexOf(' ');

				modString	=modString.Substring(0, endIdx);

				int	modID	=0;
				if(!int.TryParse(modString, out modID))
				{
					Console.WriteLine("Failed to grab download dir...");
					return;
				}

				int	toIDX	=drea.Data.IndexOf("to \"");
				if(toIDX <= 0)
				{
					Console.WriteLine("Failed to grab download dir...");
					return;
				}

				endIdx	=drea.Data.IndexOf("\" ", toIDX + 4);
				if(endIdx <= 0)
				{
					Console.WriteLine("Failed to grab download dir...");
					return;
				}

				string	path	=drea.Data.Substring(toIDX + 4, endIdx - (toIDX + 4));

//				Console.WriteLine("Mod " + modID + " downloaded to: " + path);

				if(!sModPaths.ContainsKey(modID))
				{
					sModPaths.Add(modID, path);
				}
			}
		}

		static void OnProcessError(object sender, EventArgs ea)
		{
			DataReceivedEventArgs	drea	=ea as DataReceivedEventArgs;
			if(drea == null)
			{
				return;
			}
			Console.WriteLine(drea.Data);
		}

		static void OnProcessExit(object sender, EventArgs ea)
		{
			Process	proc	=sender as Process;

			proc.OutputDataReceived	-=OnProcessSpew;
			proc.ErrorDataReceived	-=OnProcessSpew;
			proc.Exited				-=OnProcessExit;
		}


		static Process FireProcess(string procPath, string args)
		{
			Process	proc	=new Process();

			proc.StartInfo.FileName					=procPath;
			proc.StartInfo.Arguments				=args;
			proc.StartInfo.UseShellExecute			=false;
			proc.StartInfo.RedirectStandardOutput	=true;
			proc.StartInfo.RedirectStandardError	=true;
			proc.StartInfo.RedirectStandardInput	=true;
			proc.StartInfo.CreateNoWindow			=true;
			proc.EnableRaisingEvents				=true;

			proc.OutputDataReceived	+=OnProcessSpew;
			proc.ErrorDataReceived	+=OnProcessError;
			proc.Exited				+=OnProcessExit;

			proc.Start();
			proc.BeginOutputReadLine();
			proc.BeginErrorReadLine();

			return	proc;
		}


		//quotify string
		static string QS(string inStr)
		{
			if(!inStr.StartsWith("\""))
			{
				inStr	="\"" + inStr;
			}
			if(!inStr.EndsWith("\""))
			{
				inStr	=inStr + "\"";
			}
			return	inStr;
		}


		public static string StripExtension(string fileName)
		{
			int	dotPos	=fileName.LastIndexOf('.');
			if(dotPos != -1)
			{
				return	fileName.Substring(0, dotPos);
			}
			return	fileName;
		}
	}
}