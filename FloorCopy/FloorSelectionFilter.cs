using System;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI.Selection;

namespace FloorCopy
{
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