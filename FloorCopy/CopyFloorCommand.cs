using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using FloorCopy.Properties;

namespace FloorCopy
{
    [Transaction(TransactionMode.Manual)]
    public class CopyFloorCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
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
                    CopyFloorUsingBooleanOperations(floor);

                // we have to commit before create opening
                // because Revit creates floor geometry  only 
                // after commit transaction
                t.Commit();

                t.Start("Get temp floors");
                List<Floor> tempFloors =
                    CreateTempFloors(newFloor, floor);
                t.Commit();
                    
                // create openings
                if (tempFloors.Count > 0)
                {
                    t.Start(Resources.CreateNewFloorOpenings);

                    CreateOpeningsInNewFloor(newFloor, tempFloors);
                }
                newFloor.Location.Move(new XYZ(0, 0, 10));


                t.Commit();
            }

            return Result.Succeeded;
        }

        private void CreateOpeningsInNewFloor(Floor newFloor, List<Floor> tempFloors)
        {
            var newFloorSolid =
                newFloor.get_Geometry(new Options())
                    .First() as Solid;

            foreach (var tempFloor in tempFloors)
            {
                var tempFloorSolid =
                    tempFloor
                        .get_Geometry(new Options())
                        .First() as Solid;

                BooleanOperationsUtils
                    .ExecuteBooleanOperationModifyingOriginalSolid(
                    newFloorSolid,
                    tempFloorSolid,
                    BooleanOperationsType.Difference);
            }
        }

        private List<Floor> CreateTempFloors(Floor newFloor, Floor floor)
        {
            var floorGeometry =
              floor.get_Geometry(new Options());

            var floorSolid =
                floorGeometry.First() as Solid;

            var topFace = SolidUtils.GetTopFace(floorSolid);

            if (topFace == null)
                throw new NotSupportedException(Resources.FloorDoesNotHaveTopFace);

            if (topFace.EdgeLoops.IsEmpty)
                throw new NotSupportedException(Resources.FloorTopFateDoesNotHaveEdges);

            List<Floor> tempFloors = new List<Floor>();

            if (topFace.EdgeLoops.Size > 0)
            {
                for (int i = 1; i < topFace.EdgeLoops.Size; i++)
                {
                    var openingEdges =
                        topFace.EdgeLoops.get_Item(i);

                    var openingCurveArray =
                        GetCurveArrayFromEdgeArary(openingEdges);

                    /*
                     * Create a new floor using inner boundary
                     */
                    var tempFloor =
                        newFloor
                            .Document
                            .Create
                            .NewFloor(openingCurveArray,
                                newFloor.FloorType,
                                newFloor.Level,
                                false);

                    tempFloors.Add(tempFloor);
                }
            }

            return tempFloors;
        }

        

        private Floor CopyFloorUsingBooleanOperations(Floor floor)
        {
            var floorGeometry =
                floor.get_Geometry(new Options());

            var floorSolid =
                floorGeometry.First() as Solid;

            var topFace = SolidUtils.GetTopFace(floorSolid);

            if (topFace == null)
                throw new NotSupportedException(Resources.FloorDoesNotHaveTopFace);

            if (topFace.EdgeLoops.IsEmpty)
                throw new NotSupportedException(Resources.FloorTopFateDoesNotHaveEdges);

            /*
             * Get the first boundary of the floor.
             * This is the outer boundary
             */
            var outerBoundary =
                topFace.EdgeLoops.get_Item(0);

            /*
             * We get EdgeArray, but to create new floor we need 
             * CurveArry
             */
            var outerBoundaryCurveArray =
                GetCurveArrayFromEdgeArary(outerBoundary);

            var newFloor =
                floor
                    .Document
                    .Create
                    .NewFloor(outerBoundaryCurveArray,
                        floor.FloorType,
                        floor.Level,
                        false);

            return newFloor;
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
    }
}