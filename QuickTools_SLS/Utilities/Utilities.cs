using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Geometry.Intersect;

namespace QuickTools_SLS.Utilities
{
    public class Utilities
    {
        public List<System.Guid> CreatePipesFromCurves(RhinoDoc doc, List<Guid> MPGuid, List<Rhino.Geometry.Line> lines, double pipeRadius, int pipeSegments)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                Rhino.Geometry.Line MTE = lines[i];
                Rhino.Geometry.LineCurve MTE_ = new Rhino.Geometry.LineCurve(MTE);
                Rhino.Geometry.Mesh meshPipe = Rhino.Geometry.Mesh.CreateFromCurvePipe(MTE_, pipeRadius, pipeSegments, 1, Rhino.Geometry.MeshPipeCapStyle.Flat, true, null);
                Guid meshPipeGuid = doc.Objects.AddMesh(meshPipe);
                MPGuid.Add(meshPipeGuid);
            }
            return MPGuid;
        }
        


        public List<System.Guid> CreatePipesFromCurves(RhinoDoc doc, List<Guid> MPGuid, Rhino.Geometry.Collections.MeshTopologyEdgeList lines, double pipeRadius, int pipeSegments)
        {
            for (int i = 0; i < lines.Count; i++)
            {
                Rhino.Geometry.Line MTE = lines.EdgeLine(i);
                Rhino.Geometry.LineCurve MTE_ = new Rhino.Geometry.LineCurve(MTE);
                Rhino.Geometry.Mesh meshPipe = Rhino.Geometry.Mesh.CreateFromCurvePipe(MTE_, pipeRadius, pipeSegments, 1, Rhino.Geometry.MeshPipeCapStyle.Flat, true, null);
                Guid meshPipeGuid = doc.Objects.AddMesh(meshPipe);
                MPGuid.Add(meshPipeGuid);
            }
            return MPGuid;
        }

        public Rhino.Geometry.Point3d FindClosestMeshVertex(Rhino.Geometry.Mesh mesh, Rhino.Geometry.Point3d point)
        {
            double minDistance = double.MaxValue;
            int minIndex = -1;
            for (int i = 0; i < mesh.Vertices.Count; i++)
            {
                double distance = mesh.Vertices[i].DistanceTo((Rhino.Geometry.Point3f)point);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    minIndex = i;
                }
            }
            return (Rhino.Geometry.Point3d)mesh.Vertices[minIndex];
        }
        
        public Rhino.Geometry.Mesh EnsureMesh(ObjRef geo0)
        {
            Rhino.Geometry.Mesh mesh = geo0.Mesh();
            if (mesh == null)
            {
                Rhino.Geometry.Brep brep = geo0.Brep();
                MeshingParameters meshPara = MeshingParameters.QualityRenderMesh;
                //meshPara.MaximumEdgeLength = 1.0;
                Rhino.Geometry.Mesh[] geo0_ = Rhino.Geometry.Mesh.CreateFromBrep(brep, meshPara);
                //geo0 = geo1_[0];

                Rhino.Geometry.Mesh joinedMesh = new Rhino.Geometry.Mesh();
                foreach (var geo0_Ind in geo0_)
                {
                    joinedMesh.Append(geo0_Ind);
                }
                joinedMesh.Weld(Math.PI);
                return joinedMesh;
            }
            else
            {
                return mesh;
            }
        }
    }
}