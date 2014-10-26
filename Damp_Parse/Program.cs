using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Damp_Parse
{
	class Program
	{
		static private string currString;
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
						Console.WriteLine("/* Data generated " + DateTime.Now.ToString("G") + " from \"" + args[0] + "\" */");

						while (sr.Peek() >= 0)
						{
							currString = sr.ReadLine().Trim();

							if (currString.StartsWith("Loading Patch "))
							{
								if (inProgrammingBlock)
								{
									// Error! end of block not found
									Console.WriteLine("#error End of block not found!");
									Console.WriteLine("");
								}
								inProgrammingBlock = true;
							}
							else if (currString.EndsWith(" Loaded Successfully"))
							{
								if(inProgrammingBlock)
								{
									inProgrammingBlock = false;

									parsedLines.Add("");
									parsedLines.Add("static const I2cIf_SequenceJobCfgType I2cIf_Sequence_DAMP_Prog" + blockCount + "_Jobs[] =");
									parsedLines.Add("{");

									for(int i = 0; i < dataLineCount; i++)
									{
										parsedLines.Add("\t{ I2CIF_JOB_WRITE, sizeof(Damp_ProgFile" + blockCount + "_" + i + "), (uint8 *)&Damp_ProgFile" + blockCount + "_" + i + "[0] },");
									}
									parsedLines[parsedLines.Count - 1] = parsedLines[parsedLines.Count - 1].TrimEnd(',');
									parsedLines.Add("};");
									parsedLines.Add("");

									// Output the completed data
									foreach (string line in parsedLines)
									{
										Console.WriteLine(line);
									}

									parsedLines = new List<string>();
									blockCount++;
									dataLineCount = 0;
								}
								else
								{
									// Error! start of block not found
									Console.WriteLine("#error Start of block not found!");
									Console.WriteLine("");
								}
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

								parsedLines.Add("static const uint8 Damp_ProgFile" + blockCount + "_"+ dataLineCount + "[] = {" + currString + "};");

								dataLineCount++;
							}
						}

						if(inProgrammingBlock)
						{
							// Error! end of block not found
							Console.WriteLine("#error End of block not found!");
							Console.WriteLine("");
						}

						parsedLines = new List<string>();

						for (int i = 0; i < blockCount; i++)
						{
							parsedLines.Add("#define I2CIF_N_SEQUENCE_DAMP" + i + "_JOBS (sizeof(I2cIf_Sequence_DAMP_Prog" + i + "_Jobs) / sizeof(I2cIf_SequenceJobCfgType))");
						}

						parsedLines.Add("static const I2cIf_SequenceCfgType I2cIf_Sequences[] =");
						parsedLines.Add("{");
						for (int i = 0; i < blockCount; i++)
						{
							parsedLines.Add("\t{");
							parsedLines.Add("\t\tI2CIF_N_SEQUENCE_DAMP" + i + "_JOBS,");
							parsedLines.Add("\t\t&I2cIf_Sequence_DAMP_Prog" + i + "_Jobs[0]");
							parsedLines.Add("\t},");
						}
						parsedLines[parsedLines.Count - 1] = parsedLines[parsedLines.Count - 1].TrimEnd(',');
						parsedLines.Add("};");

						// Output the completed data
						foreach (string line in parsedLines)
						{
							Console.WriteLine(line);
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
