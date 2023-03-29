using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;

namespace QuickTools_SLS.Commands
{
    public class CreateRunner : Rhino.Commands.Command
    {

        /* Calulcates BoundingBox volume for selected geometry and associated material costs
        */

        public CreateRunner()
        {
            Instance = this;
        }

        public static CreateRunner Instance { get; private set; }

        public override string EnglishName => "CreateRunner";

        public override Guid Id
        {
            get
            {
                return new Guid("{ECA66C0D-B8F8-4385-B35E-345904B9C727}");
            }
        }
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //initialize Utilities to use utility functions
            Utilities.Utilities Util = new Utilities.Utilities();
            List<Line> gatesLines = new List<Line>();
            List<Line> subRunnerLines = new List<Line>();
            List<Point3d> subRunnerBases = new List<Point3d>();
            List<Point3d> subRunnerTops = new List<Point3d>();

            Rhino.Input.Custom.GetObject getGeo = new Rhino.Input.Custom.GetObject();
            getGeo.SetCommandPrompt("Please select the Geometry to connect with a runner");

            OptionDouble RunnerDistance = new OptionDouble(2, 0.0, 10.0); //Distance of main runner from lowest object, in mm
            OptionDouble RunnerRodRadius = new OptionDouble(0.5, 0.0, 10.0); //Rod radius for sprue generation, in mm
            OptionDouble GateSize = new OptionDouble(0.2, 0.0, 10.0); //Distance from part to runner, 'height' of the gate, in mm
            OptionDouble GateRadius = new OptionDouble(0.5, 0.0, 10.0); //Gate radius at attachment point, in mm
            OptionInteger RunnerSegments = new OptionInteger(8, 3, 24); //cost in DKK per cubic centimetre
            //string[] listValues = new string[] { "Item0", "Item1", "Item2", "Item3", "Item4" };

            getGeo.AddOptionDouble("RunnerDistance", ref RunnerDistance);
            getGeo.AddOptionDouble("RodRadius", ref RunnerRodRadius);
            getGeo.AddOptionDouble("GateHeight", ref GateSize);
            getGeo.AddOptionDouble("GateRadius", ref GateRadius);
            getGeo.AddOptionInteger("RodSegmentCount", ref RunnerSegments);
            getGeo.GroupSelect = true; 
            //getGeo.GetMultiple(1, 0);
            getGeo.GeometryFilter = ObjectType.Mesh | ObjectType.Brep; //Should also include: ObjectType.Brep |

            //int listIndex = 3;
            //int opList = getGeo.AddOptionList("List", listValues, listIndex);
            
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


                        //GeometryBase geo0 = getGeo.Object(i).Geometry();
                        BoundingBox bboxTemp;
                        Rhino.Geometry.Mesh geoE = Util.EnsureMesh(getGeo.Object(i));
                        bboxTemp = geoE.GetBoundingBox(true);

                        Point3d bboxCent = bboxTemp.Center;
                        Point3d bboxMin = bboxTemp.Min;
                        Point3d centPtAdj = new Point3d(bboxCent.X, bboxCent.Y, bboxMin.Z);

                        Point3d centPtAdj_ = Util.FindClosestMeshVertex(geoE, centPtAdj);
                        Point3d gateLineBase = new Point3d(centPtAdj_.X, centPtAdj_.Y, centPtAdj_.Z - GateSize.CurrentValue);

                        double runnerDistAdj = RunnerDistance.CurrentValue;
                        if (runnerDistAdj < (GateSize.CurrentValue + RunnerRodRadius.CurrentValue))
                        {
                            runnerDistAdj = GateSize.CurrentValue + RunnerRodRadius.CurrentValue;
                        }

                        Point3d runnerLineBase = new Point3d(centPtAdj_.X, centPtAdj_.Y, centPtAdj_.Z - runnerDistAdj);
                        Line gateLine = new Line(gateLineBase, centPtAdj_);
                        gatesLines.Add(gateLine);

                        Line subRunnerLine = new Line(runnerLineBase, gateLineBase);
                        subRunnerBases.Add(runnerLineBase);
                        subRunnerTops.Add(gateLineBase);
                    }

                    //loop through all points in list subRunnerBases
                    double lowestZ = double.MaxValue;
                    for (int i = 0; i < subRunnerBases.Count; i++)
                    {
                        if (i == 0)
                        {
                            lowestZ = subRunnerBases[i].Z;
                        }
                        else
                        {
                            if (subRunnerBases[i].Z <= lowestZ)
                            {
                                lowestZ = subRunnerBases[i].Z;
                            }
                        }
                    }
                    for (int i = 0; i < subRunnerBases.Count; i++)
                    {
                        subRunnerBases[i] = new Point3d(subRunnerBases[i].X, subRunnerBases[i].Y, lowestZ);
                        subRunnerLines.Add(new Line(subRunnerBases[i], subRunnerTops[i]));
                    }

                    Plane projPlane = Plane.WorldXY;
                    projPlane.OriginZ = lowestZ;

                    List<Guid> MPGuid = new List<Guid>();

                    if (subRunnerBases.Count >= 2)
                    {
                        if (subRunnerBases.Count > 2)
                        {
                            Rhino.Geometry.Mesh tessMesh = Rhino.Geometry.Mesh.CreateFromTessellation(subRunnerBases, null, projPlane, false);
                            MPGuid = Util.CreatePipesFromCurves(doc, MPGuid, tessMesh.TopologyEdges, RunnerRodRadius.CurrentValue, RunnerSegments.CurrentValue);
                        }
                        else if (subRunnerBases.Count == 2)
                        {
                            Line singleRunner = new Line(subRunnerBases[0], subRunnerBases[1]);
                            List<Line> Line_ = new List<Line>();
                            Line_.Add(singleRunner);
                            MPGuid = Util.CreatePipesFromCurves(doc, MPGuid, Line_, RunnerRodRadius.CurrentValue, RunnerSegments.CurrentValue);
                        }

                        MPGuid = Util.CreatePipesFromCurves(doc, MPGuid, subRunnerLines, RunnerRodRadius.CurrentValue, RunnerSegments.CurrentValue);
                        MPGuid = Util.CreatePipesFromCurves(doc, MPGuid, gatesLines, RunnerRodRadius.CurrentValue, RunnerSegments.CurrentValue);

                        doc.Groups.Add("MPs", MPGuid);
                    }
                    else
                    {
                        RhinoApp.WriteLine("Can't create runner, more than one geometry needs to be selected");
                    }
                }
                else if (get_rc == Rhino.Input.GetResult.Option)
                {
                    /*if (getGeo.OptionIndex() == opList)
                        listIndex = getGeo.Option().CurrentListOptionIndex;
                    */
                    continue;
                }
                
                break;
            }
            doc.Views.Redraw();
            return Result.Success;
        }
    }
}
