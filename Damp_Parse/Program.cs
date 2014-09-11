using System;
using System.Collections.Generic;
using System.IO;

namespace Damp_Parse
{
	class Program
	{
		static private string currString;
		static List<string> parsedLines = new List<string>();

		static void Main(string[] args)
		{
			// Check for correct arguments
			if(args.Length != 1)
			{
				Console.WriteLine("Usage: " + System.AppDomain.CurrentDomain.FriendlyName + " <input file>");
				return;
			}

			try
			{
				if (File.Exists(args[0]))
				{
					using (StreamReader sr = new StreamReader(args[0]))
					{
						parsedLines.Add("static const uint8 dampCommands {");

						while (sr.Peek() >= 0)
						{
							currString = sr.ReadLine().Trim();

							if ((currString.StartsWith("S D8 a")) && (currString.EndsWith(" P")))
							{
								// Write command data
								currString = currString.Remove(0, currString.IndexOf("a") + 1);	// Remove the header
								currString = currString.TrimEnd(' ', 'P');						// Remove the footer
								currString = currString.Replace(" ", ", 0x");					// Replace all spaces to make is C-style hex values
								currString = currString.Remove(0, currString.IndexOf("0x"));	// Remove extra hex chuff at the start

								parsedLines.Add("\t{" + currString + "},");
							}
							else if ((currString.StartsWith("S D9 a")) && (currString.EndsWith(" P")))
							{
								// Read needs to be performed
								parsedLines.Add("/* Perform Read Here */");
							}
						}

						// Remove the end comma from the last data line
						for(int i = parsedLines.Count - 1; i > 0; i--)
						{
							if(parsedLines[i].EndsWith("},"))
							{
								parsedLines[i] = parsedLines[i].TrimEnd(',');
								break;
							}
						}

						parsedLines.Add("};");
					}

					// Output the completed data
					foreach(string line in parsedLines)
					{
						Console.WriteLine(line);
					}
				}
				else
				{
					Console.WriteLine("File does not exist");
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("The process failed: {0}", e.ToString());
			}
		}
	}
}
