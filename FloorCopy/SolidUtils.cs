using System;
using Autodesk.Revit.DB;

namespace FloorCopy
{
    public static class SolidUtils
    {
        const double _eps = 1.0e-9;

        public static Face GetTopFace(Solid solid)
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
}