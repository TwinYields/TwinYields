using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OSGeo.GDAL;
using OSGeo.OGR;
using OSGeo.OSR;

namespace TwinYields;

public class GDALUtils
{
    public static void RasterizeFeatureCollection(NetTopologySuite.Features.FeatureCollection features, string fileName)
    {
        Ogr.RegisterAll();
        var json = AdaptConverter.ToJSON(features);
        var dsv = Ogr.Open(json, 0);
        var names = features.First().Attributes.GetNames();
        var layer = dsv.GetLayerByIndex(0);
        gdal_rasterize(layer, names, fileName);
    }

    //Trying to match output of gdal_rasterize command line tool
    public static void gdal_rasterize(Layer layer, string[] bandnames, string fileName, double pixelsize = 1e-5)
    {
        //Get data range and size
        var envelope = new Envelope();
        layer.GetExtent(envelope, 1);
        //Add space around the raster
        var ws = 10*pixelsize;
        envelope.MinX -= ws;
        envelope.MinY -= ws;
        envelope.MaxX += ws;
        envelope.MaxY += ws;
        var rx = envelope.MaxX - envelope.MinX;
        var ry = envelope.MaxY - envelope.MinY;

        int w1 = (int)(rx * 1/pixelsize);
        int h1 = (int)(ry * 1/pixelsize);
        int N = bandnames.Count();

        //Create dataset
        OSGeo.GDAL.Driver drv = Gdal.GetDriverByName("GTiff");
        Dataset dsr = drv.Create(fileName, w1, h1, N, DataType.GDT_Int32, null);
        
        //Set geometry
        string wkt = "";
        Osr.GetWellKnownGeogCSAsWKT("EPSG:4326", out wkt);
        dsr.SetProjection(wkt);
        double[] argin = new double[] { envelope.MinX, pixelsize, 0, envelope.MaxY, 0, -pixelsize};
        dsr.SetGeoTransform(argin);

        //Write bands
        for (int i=0; i<N; i++)
        {
            var bnd = dsr.GetRasterBand(i+1);
            bnd.SetNoDataValue(0);
            bnd.SetDescription(bandnames[i]);
            var rasterizeOptions = new string[] { "ATTRIBUTE=" + bandnames[i]};
            Gdal.RasterizeLayer(dsr, 1, new int[] { i+1 }, layer, IntPtr.Zero, IntPtr.Zero,
                    burn_values: 1, burn_values_list: new double[] { 255}, rasterizeOptions, null, null);
        }

        //Not actually quite sure what all is needed to close the file
        dsr.FlushCache();
        dsr.Dispose();
        drv.Dispose();
    }

}

