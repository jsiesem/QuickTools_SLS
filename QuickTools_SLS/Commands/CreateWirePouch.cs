using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using MIConvexHull;
using System.Linq;
using Rhino.Render.ChangeQueue;

namespace QuickTools_SLS.Commands
{
    public class CreateWirePouch : Rhino.Commands.Command
    {

        /* Calulcates BoundingBox volume for selected geometry and associated material costs
        */

        public CreateWirePouch()
        {
            Instance = this;
        }

        public static CreateWirePouch Instance { get; private set; }

        public override string EnglishName => "CreateWirePouch";

        public override Guid Id
        {
            get
            {
                return new Guid("{B806EA3A-6C77-4D3E-AB02-4C51C41569F0}");
            }
        }
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Utilities.Utilities Util = new Utilities.Utilities(); //initialize Utilities to use utility functions
            
            List<Point3d> points = new List<Point3d>();
            List<List<IVertex>> pointsCoordinates = new List<List<IVertex>>();
            List<Point3d> xyzTemp = new List<Point3d>();
            
            Rhino.Input.Custom.GetObject getGeo = new Rhino.Input.Custom.GetObject();

            OptionDouble QR_TQCFactor = new OptionDouble(16, 0.0, 1000); //cost in DKK per cubic centimetre
            OptionDouble MP_radius = new OptionDouble(0.5, 0.0, 10.0); //cost in DKK per cubic centimetre
            OptionInteger MP_segments = new OptionInteger(8, 3, 24); //cost in DKK per cubic centimetre
            OptionDouble hullOffset = new OptionDouble(3, 0.0, 10.0); //cost in DKK per cubic centimetre
            //Rhino.Input.GetResult get_rc = getGeo.Get();
            //using (GetObject getGeo = new GetObject())
            //{
            getGeo.AddOptionDouble("QR_TQCFactor", ref QR_TQCFactor);
            getGeo.AddOptionDouble("MeshPipeRadius", ref MP_radius);
            getGeo.AddOptionInteger("PipeSegmentCount", ref MP_segments);
            getGeo.AddOptionDouble("ConverxHullOffset", ref hullOffset);

            getGeo.SetCommandPrompt("Please select the Geometry (or Group) to enclose in a wirepouch");
            getGeo.GroupSelect = true;
            
            getGeo.GeometryFilter = ObjectType.Mesh | ObjectType.Brep; //Should also include: ObjectType.Brep |
            
            while(true)
            {
                Rhino.Input.GetResult get_rc = getGeo.GetMultiple(1, 0);
                
                if (getGeo.CommandResult() != Result.Success)
                {
                    RhinoApp.WriteLine("No geometry was selected.");
                    return getGeo.CommandResult();
                }

                if (get_rc == Rhino.Input.GetResult.Object)
                {
                    //add all mesh vertices to points
                    List<Rhino.Geometry.Mesh> meshList = new List<Rhino.Geometry.Mesh>();
                    for (int i = 0; i < getGeo.ObjectCount; i++)
                    {
                        Rhino.Geometry.Mesh geoE = Util.EnsureMesh(getGeo.Object(i));
                        meshList.Add(geoE);

                    }
                    for (int i = 0; i < meshList.Count; i++)
                    {
                        for (int j = 0; j < meshList[i].Vertices.Count; j++)
                        {
                            points.Add(meshList[i].Vertices[j]);
                        }
                    }

                    var vertices = new double[points.Count][];

                    for (int i = 0; i < points.Count; i++)
                    {
                        var location = new double[3];
                        location[0] = points[i].X;
                        location[1] = points[i].Y;
                        location[2] = points[i].Z;

                        vertices[i] = location;

                    }
                    //Compute convex hull
                    var convexHull = ConvexHull.Create(vertices);
                    double[][] hullPoints = convexHull.Points.Select(p => p.Position).ToArray();
                    var test = hullPoints.GetLength(0);
                    for (int i = 0; i < hullPoints.GetLength(0); i++)
                    {
                        var test3 = hullPoints[i];
                        var test4 = hullPoints[i][0];
                        Point3d ptNew = new Point3d(hullPoints[i][0], hullPoints[i][1], hullPoints[i][2]);
                        xyzTemp.Add(ptNew);
                    }

                    Rhino.Geometry.Mesh hullMesh = new Rhino.Geometry.Mesh();
                    int vertexCount = 0;
                    List<PolylineCurve> faceCrvAll = new List<PolylineCurve>();
                    List<Line> edgeLines = new List<Line>();

                    IVertex[][] hullFaces = convexHull.Faces.Select(f => f.Vertices).ToArray();

                    for (int i = 0; i < hullFaces.GetLength(0); i++)
                    {
                        List<Point3d> ptSet = new List<Point3d>();
                        var hullFaceTemp = hullFaces[i];
                        for (int j = 0; j < 3; j++)
                        {
                            Point3d xyz = new Point3d(hullFaceTemp[j].Position[0], hullFaceTemp[j].Position[1], hullFaceTemp[j].Position[2]);
                            hullMesh.Vertices.Add(xyz);
                            ptSet.Add(xyz);
                        }
                        hullMesh.Faces.AddFace(vertexCount, vertexCount + 1, vertexCount + 2);
                        ptSet.Add(ptSet[0]);
                        PolylineCurve crvTemp = new PolylineCurve(ptSet);
                        faceCrvAll.Add(crvTemp);
                        vertexCount += 3;
                    }

                    hullMesh.Weld(Math.PI);
                    hullMesh.Normals.ComputeNormals();
                    hullMesh.Compact();

                    for (int i = 0; i < hullMesh.Vertices.Count; i++)
                    {
                        Vector3d normal = hullMesh.Normals[i];
                        Point3d vertexPos = hullMesh.Vertices[i];
                        hullMesh.Vertices.SetVertex(i, vertexPos + normal * hullOffset.CurrentValue);
                    }

                    Guid hullMeshGuid = doc.Objects.Add(hullMesh);

                    double meshArea = AreaMassProperties.Compute(hullMesh).Area;
                    int calcTQC = Convert.ToInt32(Math.Round(meshArea / QR_TQCFactor.CurrentValue));

                    ObjRef mesh2 = new ObjRef(doc, hullMeshGuid);
                    Rhino.Geometry.Mesh mesh2_ = mesh2.Mesh();

                    QuadRemeshParameters qr_params = new QuadRemeshParameters();
                    qr_params.AdaptiveQuadCount = true;
                    qr_params.TargetQuadCount = calcTQC;
                    qr_params.AdaptiveSize = 50;
                    qr_params.DetectHardEdges = true;
                    qr_params.SymmetryAxis = 0;
                    Rhino.Geometry.Mesh hullQR = mesh2_.QuadRemesh(qr_params);

                    hullQR.Normals.ComputeNormals();
                    hullQR.Compact();

                    var meshGUID = doc.Objects.Add(hullQR);
                    if (meshGUID != Guid.Empty)
                    {
                        RhinoApp.WriteLine("Mesh GUID is {0}", meshGUID);
                        RhinoApp.WriteLine("mesh has {0} vertices", hullQR.Vertices.Count());
                        RhinoApp.WriteLine("{0}", hullQR);

                        List<Guid> MPGuid = new List<Guid>();
                        MPGuid = Util.CreatePipesFromCurves(doc, MPGuid, hullQR.TopologyEdges, MP_radius.CurrentValue, MP_segments.CurrentValue);
                        doc.Groups.Add("MPs", MPGuid);

                        doc.Objects.Delete(meshGUID, false);
                    }
                    else
                    {
                        RhinoApp.WriteLine("Coulndt draw quad mesh, attempting to draw initial mesh instead");
                        doc.Objects.AddMesh(hullMesh);
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
        //}
        doc.Views.Redraw();
        return Result.Success;
        }
    }
}
