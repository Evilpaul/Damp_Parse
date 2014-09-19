using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Damp_Parse
{
	class Program
	{
		static private string currString;
		static private int maxLineCount = 0;
		static private int dataLineCount = 0;
		static List<string> parsedLines = new List<string>();
		static private int blockCount = 0;
		static private bool inProgrammingBlock = false;

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
						while (sr.Peek() >= 0)
						{
							currString = sr.ReadLine().Trim();

							if (currString.StartsWith("Loading Patch "))
							{
								inProgrammingBlock = true;
							}
							else if (currString.EndsWith(" Loaded Successfully"))
							{
								inProgrammingBlock = false;
								// Remove the end comma from the last data line
								for (int i = parsedLines.Count - 1; i > 0; i--)
								{
									if (parsedLines[i].EndsWith("},"))
									{
										parsedLines[i] = parsedLines[i].TrimEnd(',');
										break;
									}
								}

								parsedLines.Insert(0, "#define DAMP_PROG_CMD_COUNT" + blockCount + " (" + dataLineCount + ")");
								parsedLines.Insert(1, "#define DAMP_PROG_CMD_LENGTH" + blockCount + " (" + maxLineCount + ")");
								parsedLines.Insert(2, "static const uint8 dampProgBlock" + blockCount + "[DAMP_PROG_CMD_COUNT" + blockCount + "][DAMP_PROG_CMD_LENGTH" + blockCount + "] = {");
								parsedLines.Add("};");
								parsedLines.Add("");

								// Output the completed data
								foreach (string line in parsedLines)
								{
									Console.WriteLine(line);
								}

								parsedLines = new List<string>();
								blockCount++;
								maxLineCount = 0;
								dataLineCount = 0;
							}
							else if ((currString.StartsWith("S D8 a")) && (currString.EndsWith(" P")) && inProgrammingBlock)
							{
								// Write command data
								currString = currString.Remove(0, currString.IndexOf("a") + 1);	// Remove the header
								currString = currString.TrimEnd(' ', 'P');						// Remove the footer
								currString = currString.Replace(" ", ", 0x");					// Replace all spaces to make is C-style hex values
								currString = currString.Remove(0, currString.IndexOf("0x"));	// Remove extra hex chuff at the start

								// determine the number of bytes in the command
								int count = currString.Count(f => f == 'x');
								if (count > maxLineCount)
								{
									// store the maximum command length found
									maxLineCount = count;
								}

								dataLineCount++;

								parsedLines.Add("\t{" + currString + "},");
							}
						}
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
