using Eto.Forms;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;

namespace QuickTools_SLS.Commands
{
    public class AlignBottomToWorldZ : Rhino.Commands.Command
    {
        public AlignBottomToWorldZ()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static AlignBottomToWorldZ Instance { get; private set; }

        public override string EnglishName => "AlignBottomToWorldZ";

        public override Guid Id
        {
            get
            {
                return new Guid("{3bfc6373-0d0e-477c-9e60-74d55cbf7c91}");
            }
        }
        
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            List<int> allGroups = new List<int>();
            Rhino.Input.Custom.GetObject getGeo = new Rhino.Input.Custom.GetObject();
            getGeo.SetCommandPrompt("Please select the Geometry to align to Z = 0.0 + tolerance");

            getGeo.GeometryFilter = ObjectType.Mesh | ObjectType.Brep; //Should also include: ObjectType.Brep |
            
            while (true)
            {
                getGeo.GroupSelect = true;
                //getGeo.SubObjectSelect = false;
                Rhino.Input.GetResult get_rc = getGeo.GetMultiple(1, 0);
                
                if (getGeo.CommandResult() != Result.Success)
                {
                    RhinoApp.WriteLine("No geometry was selected.");
                    return getGeo.CommandResult();
                }

                //var groups = Rhino.DocObjects.Tables.GroupTable
                if (get_rc == Rhino.Input.GetResult.Object)
                {
                    Dictionary<int, List<Rhino.DocObjects.ObjRef>> groupDict = new Dictionary<int, List<Rhino.DocObjects.ObjRef>>();
                    for (int i = 0; i < getGeo.ObjectCount; i++)
                    {

                        Rhino.DocObjects.ObjRef geo0 = getGeo.Object(i);
                        //var g = doc.Groups;
                        //BoundingBox bboxTemp = geo0.Geometry().GetBoundingBox(true);
                        int groupID = 0;
                        int[] groupMembership = geo0.Object().Attributes.GetGroupList();
                        if(groupMembership.Count() > 1)
                        {
                            RhinoApp.WriteLine("Multiple Group memberships found, using last one for convenience, please verify result before proceeding!");
                            groupID = groupMembership[groupMembership.Count() - 1] + 1;
                        }
                        else if(groupMembership.Count() == 1)
                        {
                            groupID = groupMembership[0] + 1;
                        }
                        else
                        {
                            groupID = 0;
                        }
                        if (groupDict.ContainsKey(groupID))
                        {
                            groupDict[groupID].Add(geo0);
                        }
                        else
                        {
                            List<Rhino.DocObjects.ObjRef> geoList = new List<Rhino.DocObjects.ObjRef>();
                            geoList.Add(geo0);
                            groupDict.Add(groupID, geoList);
                        }

                        //geo0.Translate(0, 0, -1 * bboxTemp.Min.Z);
                        //var xform = Rhino.Geometry.Transform.Translation(0, 0,-1 * bboxTemp.Min.Z);
                        //doc.Objects.Transform(getGeo.Object(i), xform, true);
                    }
                    
                    int[] keys = groupDict.Keys.ToArray();
                    
                    foreach(int key in keys)
                    {
                        List<Rhino.DocObjects.ObjRef> vals = groupDict[key];
                        double newZMin = GetMinBboxZ(vals);
                        SetZMin(doc, vals, newZMin, 0.01);
                    }
                }

                else if (get_rc == Rhino.Input.GetResult.Option)
                {
                    continue;
                }
                break;
            }
            RhinoApp.WriteLine("text");

            doc.Views.Redraw();
            return Result.Success;
        }
        
        public double GetMinBboxZ(List<Rhino.DocObjects.ObjRef> geo)
        {
            double bMinZ = 100000000.0; //use ActiveDocs high value
            foreach (Rhino.DocObjects.ObjRef gRef in geo)
            {
                GeometryBase g = gRef.Geometry();
                BoundingBox bboxTemp = g.GetBoundingBox(true);
                if(bboxTemp.Min.Z < bMinZ)
                {
                    bMinZ = bboxTemp.Min.Z;
                }
            }
            return bMinZ;
        }
        
        public void SetZMin(RhinoDoc doc, List<Rhino.DocObjects.ObjRef> geo, double newZMin, double toleranceConversionTolerance)
        {
            foreach(Rhino.DocObjects.ObjRef gRef in geo)
            {
                var xform = Rhino.Geometry.Transform.Translation(0, 0,(-1 * newZMin) + toleranceConversionTolerance);
                doc.Objects.Transform(gRef, xform, true);
            }
        }
        public void GetMinBboxZ(GeometryBase geo)
        {
            BoundingBox bboxTemp = geo.GetBoundingBox(true);
            double bMinZ = bboxTemp.Min.Z;
        }
    }
}