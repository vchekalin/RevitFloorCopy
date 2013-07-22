#region Namespaces
using System;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using FloorCopy.Properties;

#endregion

namespace FloorCopy
{
    [Transaction(TransactionMode.Manual)]
    public class Command : IExternalCommand
    {
        const double _eps = 1.0e-9;

        public Result Execute(
          ExternalCommandData commandData,
          ref string message,
          ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Application app = uiapp.Application;
            Document doc = uidoc.Document;

            Reference r;
            try
            {
                r = uidoc.Selection.PickObject(ObjectType.Element,
                    new FloorSelectionFilter(), Resources.SelectFloor);
            }
            catch (Exception)
            {

                return Result.Cancelled;
            }

            var floor = doc.get_Element(r.ElementId) as Floor;

            using (Transaction t =
                new Transaction(doc, Resources.CopyFloor))
            {
                t.Start();

                var newFloor =
                    CopyFloor(floor);

                var moveRes =
                    newFloor.Location.Move(new XYZ(0, 0, 10));

                t.Commit();

                t.Start(Resources.CreateNewFloorOpenings);

                CreateFloorOpenings(floor, newFloor);

                var res = t.Commit();
            }

            return Result.Succeeded;
        }

        

       
        private Floor CopyFloor1(Floor sourceFloor)
        {
            var floorGeometryElement =
                sourceFloor.get_Geometry(new Options());

            foreach (var geometryObject in floorGeometryElement)
            {
                var floorSolid =
                    geometryObject as Solid;

                if (floorSolid == null)
                    continue;

                var topFace =
                    GetTopFace(floorSolid);

                if (topFace == null)
                    throw new NotSupportedException(Resources.FloorDoesNotHaveTopFace);

                if (topFace.EdgeLoops.IsEmpty)
                    throw new NotSupportedException(Resources.FloorTopFateDoesNotHaveEdges);

                var outerBoundary =
                    topFace.EdgeLoops.get_Item(0);

                // create new floor using source floor outer boundaries

                CurveArray floorCurveArray =
                    GetCurveArrayFromEdgeArary(outerBoundary);

                var newFloor =
                    sourceFloor
                        .Document
                        .Create
                        .NewFloor(floorCurveArray, false);


                // if source floor has openings
                if (topFace.EdgeLoops.Size > 1)
                {
                    for (int i = 1; i < topFace.EdgeLoops.Size; i++)
                    {
                        var openingEdges =
                            topFace.EdgeLoops.get_Item(i);

                        var openingCurveArray =
                            GetCurveArrayFromEdgeArary(openingEdges);

                        var opening =
                            sourceFloor
                                .Document
                                .Create
                                .NewOpening(newFloor,
                                            openingCurveArray,
                                            true);
                    }
                }

                return newFloor;
            }

            return null;
        }

        private void CreateFloorOpenings(Floor sourceFloor, Floor destFloor)
        {
            // looking if source floor has openings

            var floorGeometryElement =
               sourceFloor.get_Geometry(new Options());

            foreach (var geometryObject in floorGeometryElement)
            {
                var floorSolid =
                    geometryObject as Solid;

                if (floorSolid == null)
                    continue;

                var topFace =
                    GetTopFace(floorSolid);

                if (topFace == null)
                    throw new NotSupportedException(Resources.FloorDoesNotHaveTopFace);

                if (topFace.EdgeLoops.IsEmpty)
                    throw new NotSupportedException(Resources.FloorTopFateDoesNotHaveEdges);


                // if source floor has openings
                if (topFace.EdgeLoops.Size > 1)
                {
                    for (int i = 1; i < topFace.EdgeLoops.Size; i++)
                    {
                        var openingEdges =
                            topFace.EdgeLoops.get_Item(i);

                        var openingCurveArray =
                            GetCurveArrayFromEdgeArary(openingEdges);

                        var opening =
                            sourceFloor
                                .Document
                                .Create
                                .NewOpening(destFloor,
                                            openingCurveArray,
                                            true);
                    }
                }

            }
        }


        private Floor CopyFloor(Floor sourceFloor)
        {
            var floorGeometryElement =
                sourceFloor.get_Geometry(new Options());

            foreach (var geometryObject in floorGeometryElement)
            {
                var floorSolid =
                    geometryObject as Solid;

                if (floorSolid == null)
                    continue;

                var topFace =
                    GetTopFace(floorSolid);

                if (topFace == null)
                    throw new NotSupportedException(Resources.FloorDoesNotHaveTopFace);

                if (topFace.EdgeLoops.IsEmpty)
                    throw new NotSupportedException(Resources.FloorTopFateDoesNotHaveEdges);

                var outerBoundary =
                    topFace.EdgeLoops.get_Item(0);

                // create new floor using source floor outer boundaries

                CurveArray floorCurveArray =
                    GetCurveArrayFromEdgeArary(outerBoundary);

                var newFloor =
                    sourceFloor
                        .Document
                        .Create
                        .NewFloor(floorCurveArray, false);

                /*
                // if source floor has openings
                if (topFace.EdgeLoops.Size > 1)
                {
                    for (int i = 1; i < topFace.EdgeLoops.Size; i++)
                    {
                        var openingEdges =
                            topFace.EdgeLoops.get_Item(i);

                        var openingCurveArray =
                            GetCurveArrayFromEdgeArary(openingEdges);

                        var opening =
                            sourceFloor
                                .Document
                                .Create
                                .NewOpening(newFloor,
                                            openingCurveArray,
                                            true);
                    }
                }
                */

                return newFloor;
            }

            return null;
        }

        private CurveArray GetCurveArrayFromEdgeArary(EdgeArray edgeArray)
        {
            CurveArray curveArray =
                new CurveArray();

            foreach (Edge edge in edgeArray)
            {
                var edgeCurve =
                        edge.AsCurve();

                curveArray.Append(edgeCurve);
            }

            return curveArray;
        }


        PlanarFace GetTopFace(Solid solid)
        {
            PlanarFace topFace = null;
            FaceArray faces = solid.Faces;
            foreach (Face f in faces)
            {
                PlanarFace pf = f as PlanarFace;
                if (null != pf
                  && (Math.Abs(pf.Normal.X - 0) < _eps && Math.Abs(pf.Normal.Y - 0) < _eps))
                {
                    if ((null == topFace)
                      || (topFace.Origin.Z < pf.Origin.Z))
                    {
                        topFace = pf;
                    }
                }
            }
            return topFace;
        }
    }

    public class FloorSelectionFilter : ISelectionFilter
    {
        public bool AllowElement(Element elem)
        {
            return elem is Floor;
        }

        public bool AllowReference(Reference reference, XYZ position)
        {
            throw new NotImplementedException();
        }
    }
}
