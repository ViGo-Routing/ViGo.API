using Newtonsoft.Json;
using System.Drawing;
using System.Reflection;
using System.Text;

namespace ViGo.Utilities.Data
{
    internal static class DataFileUtilities
    {
        private static string hcmBoundariesFile =
            Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
            "Data\\hcm-boundaries.json");
        //private static string ReadFile(string fileName)
        //{
        //    using StreamReader r = new StreamReader(fileName);
        //    string data = r.ReadToEnd();

        //    return data;
        //}

        internal static System.Drawing.PointF[] GetHcmCityBoundaries()
        {
            using StreamReader r = new StreamReader(hcmBoundariesFile);
            string data = r.ReadToEnd();
            r.Close();

            dynamic[] boundaries = JsonConvert.DeserializeObject<dynamic[]>(data);

            System.Drawing.PointF[] result = (from boundary in boundaries
                               select new System.Drawing.PointF((float)boundary[0], (float)boundary[1])).ToArray();

            StringBuilder newBoundaryData = new StringBuilder();
            newBoundaryData.AppendLine("[");
            foreach (System.Drawing.PointF point in result)
            {
                newBoundaryData.AppendLine($"[{point.Y},{point.X}],");
            }
            newBoundaryData.Remove(newBoundaryData.Length - 1, 1);
            newBoundaryData.AppendLine("]");
            using StreamWriter w = new StreamWriter(hcmBoundariesFile, false);
            w.Write(newBoundaryData.ToString());
            w.Flush();
            return result;
        }
    }
}
