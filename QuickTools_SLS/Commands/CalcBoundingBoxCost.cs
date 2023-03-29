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
    public class CalcBoundingBoxCost : Command
    {

        /* Calulcates BoundingBox volume for selected geometry and associated material costs
        */

        public CalcBoundingBoxCost()
        {
            Instance = this;
        }

        public static CalcBoundingBoxCost Instance { get; private set; }

        public override string EnglishName => "CalcBoundingBoxCost";

        public override Guid Id
        {
            get
            {
                return new Guid("{81BEAAE8-DE20-4CFA-8376-3ED544506D4C}");
            }
        }
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            BoundingBox prevBbox = new BoundingBox();
            //Double materialCost = 0.25; //cost in DKK per cubic centimetre
            Rhino.Input.Custom.GetObject getGeo = new Rhino.Input.Custom.GetObject();
            getGeo.SetCommandPrompt("Please select the Geometry to calculate the cost of"); 
            OptionDouble priceFactor = new OptionDouble(0.25, 0.00, 100.00); //cost in DKK per cubic centimetre

            getGeo.AddOptionDouble("PricePerCm³", ref priceFactor);
            getGeo.GroupSelect = true;
            //getGeo.GetMultiple(1, 0);
            getGeo.GeometryFilter = ObjectType.Brep | ObjectType.Mesh;
            //GetResult get_rc = getGeo.Get();
            
            while (true)
            {
                Rhino.Input.GetResult get_rc = getGeo.GetMultiple(1, 0);

                if (getGeo.CommandResult() != Result.Success)
                {
                    RhinoApp.WriteLine("No geometry was selected.");
                    return getGeo.CommandResult();
                }

                if (get_rc == Rhino.Input.GetResult.Object)
                {
                    for (int i = 0; i < getGeo.ObjectCount; i++)
                    {
                        GeometryBase geo0 = getGeo.Object(i).Geometry();

                        BoundingBox bboxTemp = geo0.GetBoundingBox(true);
                        if (i == 0)
                        {
                            prevBbox = bboxTemp;
                        }
                        else
                        {
                            prevBbox = BoundingBox.Union(bboxTemp, prevBbox);
                        }
                    }
                }
                else if (get_rc == Rhino.Input.GetResult.Option)
                {
                    continue;
                }
                break;
            }
            double bboxVol = prevBbox.Volume / 1000; //in cubic centimeters
            RhinoApp.WriteLine("BoundingBox for selected objects is {0} cm³ resulting in material costs of: {1} DKK", Math.Round(bboxVol, 2), Math.Round(bboxVol * priceFactor.CurrentValue, 2));
            return Result.Success;
        }
    }
}
