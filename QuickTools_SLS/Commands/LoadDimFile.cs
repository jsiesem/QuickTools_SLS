using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace QuickTools_SLS.Commands
{
    public class LoadDimFile : Command
    {
        public LoadDimFile()
        {
            Instance = this;
        }

        public static LoadDimFile Instance { get; private set; }

        public override string EnglishName => "LoadDimFile";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            /*check for given filepaths and load the first one that exists and gives warning if file cant be found
            */

            string filename = "";
            List<string> filepaths = new List<string>
                {};

            RhinoApp.WriteLine("Loading Dim.3dm file", EnglishName);

            for (int i = 0; i < filepaths.Count; i++)
            {
                string fileLocationSuccess = RhinoDoc.ActiveDoc.FindFile(filepaths[i]);
                if (string.IsNullOrEmpty(fileLocationSuccess))
                {
                    continue;
                }
                else
                {
                    filename = fileLocationSuccess;
                    break;
                }

            }

            if (string.IsNullOrEmpty(filename))
            {
                RhinoApp.WriteLine("Dim.3dm file not found, file expected in active model folder or at any of the following locations: {0}", string.Join(", ", filepaths));
                //RhinoApp.WriteLine("You might not be connected to the S drive, or the file was moved");
            }
            else
            {
                RhinoDoc.ActiveDoc.Import(filename);
            }
            // ---
            return Result.Success;
        }
    }
}
