using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Dicom.Data;
using FMI.CT.Review.Model;

namespace ChangePixelData
{
    public class Program
    {
        static Program()
        {
            blqw.LoadResourceDll.RegistDLL();
        }
        public Dictionary<string, string> tags = new Dictionary<string, string>();
        public string fileout="C:\\TEMP.dcm"; 
        public static void Main(string[] args)
        {
           
            //DicomElement handler0 = new DicomElement();
            //handler.getImg_DefaultWindow_openfile(args[0]);
           // handler0.getImg_DefaultWindow_openfile("@345.IMA");
            //string fileStr = @"C:\Users\XSY\Desktop\changDicomTag_0708\exported0000.dcm";
            DicomElement handler1 = new DicomElement();
            handler1.getImg_DefaultWindow_openfile("exported0000.dcm");

            Program program = new Program();
            program.tags = handler1.tags;
            //if (args.Length >= 2)
            //    program.fileout = args[1];
            //else
            program.fileout = "C:\\TEMP3.dcm";
             //string str = program.MakeGreyDicom(handler1.buffer, 512, 512);
            //System.Drawing.Bitmap img = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(args[1]);
            string str = program.MakeGreyDicom(program.arrayshortToByte(handler1.ArrayRawpixData.ToArray()), 512, 512);
            //string str = program.MakeGreyDicom(program.RGB2Gray(img), (ushort)img.Width, (ushort)img.Height);
            //System.Drawing.Bitmap img = (System.Drawing.Bitmap)System.Drawing.Image.FromFile(@"@345.bmp");
            //string str = program.MakeGreyDicom(program.RGB2Gray(img), (ushort)img.Width, (ushort)img.Height);
           
        }
        public byte[] arrayshortToByte(short[] arrs)
        {
            byte[] arrb = new byte[arrs.Length * 2];

            //for (int i = 0; i < arrs.Length * 2; i += 2)
            //{
            //    short tmp = arrs[i / 2];
            //    arrb[i+1] = (byte)(tmp >> 8);
            //    arrb[i ] = (byte)(tmp & 0x00FF);
            //} 
            for (int i = 0; i < arrs.Length; i++)
            {
                //if (arrs[i]<0)
                //{
                //    arrs[i] = (short)( arrs[i]);
                //}
                byte[] bi = System.BitConverter.GetBytes(1024 + arrs[i]);
                arrb[2 * i] = bi[0];
                arrb[2 * i + 1] = bi[1];

            }
            //Buffer.BlockCopy(arrs, 0, arrb, 0, arrb.Length);
            return arrb;
        }
        public string MakeGreyDicom(byte[] greybytes, ushort imgwidth, ushort imgheight)
        {
            DcmUID studyUid = DcmUID.Generate();
            DcmUID seriesUid = DcmUID.Generate(studyUid, 1);
            DcmUID instUid = DcmUID.Generate(seriesUid, 1);
            DcmDataset data = new DcmDataset(DcmTS.ExplicitVRBigEndian);//.ImplicitVRLittleEndian  ok
            data.AddElementWithValue(DcmTags.SOPClassUID, DcmUIDs.CTImageStorage);//ComputedRadiographyImageStorage  ok
            //data.AddElementWithValue(DcmTags.SOPClassUID, DcmUIDs .SecondaryCapture); 
            data.AddElementWithValue(DcmTags.StudyInstanceUID, studyUid);
            data.AddElementWithValue(DcmTags.SeriesInstanceUID, seriesUid);
            data.AddElementWithValue(DcmTags.SOPInstanceUID, instUid);//"1.3.6.1.4.1.30071.6.635719267134010719.1.1"

            //data.AddElementWithValue(DcmTags.MediaStorageSOPClassUID, DcmUIDs.ImplicitVRLittleEndian);
            //data.AddElementWithValueString(DcmTags.MediaStorageSOPClassUID, DcmUIDs.ComputedRadiographyImageStorage.ToString());

            //type 2 attributes
            ////data.AddElement(DcmTags.PrinterStatus);
            if (tags.ContainsKey("0010,0020"))
                data.AddElementWithValueString(DcmTags.PatientID, tags["0010,0020"].Substring(5));
            if (tags.ContainsKey("0010,0010"))
                data.AddElementWithValueString(DcmTags.PatientsName, tags["0010,0010"].Substring(5));
            if (tags.ContainsKey("0010,0030"))
                data.AddElementWithValueString(DcmTags.PatientsBirthDate, tags["0010,0030"].Substring(5));
            if (tags.ContainsKey("0010,0040"))
                data.AddElementWithValueString(DcmTags.PatientsSex, tags["0010,0040"].Substring(5));
            if (tags.ContainsKey("0010,1010"))
                data.AddElementWithValueString(DcmTags.PatientsAge, tags["0010,1010"].Substring(5));
            
            if (tags.ContainsKey("0008,0005"))
                data.AddElementWithValueString(DcmTags.SpecificCharacterSet, tags["0008,0005"].Substring(5));
            if (tags.ContainsKey("0008,0008"))
                data.AddElementWithValueString(DcmTags.ImageType, tags["0008,0008"].Substring(5));
            //if (tags.ContainsKey("0008,0016"))
            //    data.AddElementWithValueString(DcmTags.ContentTime, DateTime.Now.ToString());
            //if (tags.ContainsKey("0008,0018"))
            //    data.AddElementWithValueString(DcmTags.ContentTime, DateTime.Now.ToString());
            if (tags.ContainsKey("0008,0020"))
                data.AddElementWithValueString(DcmTags.StudyDate, tags["0008,0020"].Substring(5));
            if (tags.ContainsKey("0008,0021"))
                data.AddElementWithValueString(DcmTags.SeriesDate, tags["0008,0021"].Substring(5));
            if (tags.ContainsKey("0008,0022"))
                data.AddElementWithValueString(DcmTags.AcquisitionDate, tags["0008,0022"].Substring(5));
            if (tags.ContainsKey("0008,0023"))
                data.AddElementWithValueString(DcmTags.ContentDate, tags["0008,0023"].Substring(5));
            if (tags.ContainsKey("0008,002a"))
                data.AddElementWithValueString(DcmTags.AcquisitionDateTime, tags["0008,002a"].Substring(5));
            if (tags.ContainsKey("0008,0030"))
                data.AddElementWithValueString(DcmTags.StudyTime, tags["0008,0030"].Substring(5));
            if (tags.ContainsKey("0008,0031"))
                data.AddElementWithValueString(DcmTags.SeriesTime, tags["0008,0031"].Substring(5));
            if (tags.ContainsKey("0008,0032"))
                data.AddElementWithValueString(DcmTags.AcquisitionTime, tags["0008,0032"].Substring(5));
            if (tags.ContainsKey("0008,0033"))
                data.AddElementWithValueString(DcmTags.ContentTime, tags["0008,0033"].Substring(5));

            if (tags.ContainsKey("0008,0050"))
                data.AddElementWithValueString(DcmTags.AcquisitionNumber, tags["0008,0050"].Substring(5));
            if (tags.ContainsKey("0008,0060"))
                data.AddElementWithValueString(DcmTags.Modality, tags["0008,0060"].Substring(5));
            if (tags.ContainsKey("0008,0070"))
                data.AddElementWithValueString(DcmTags.Manufacturer, tags["0008,0070"].Substring(5));
            if (tags.ContainsKey("0008,0080"))
                data.AddElementWithValueString(DcmTags.InstitutionName, tags["0008,0080"].Substring(5));
            if (tags.ContainsKey("0008,0081"))
                data.AddElementWithValueString(DcmTags.InstitutionAddress, tags["0008,0081"].Substring(5));
            if (tags.ContainsKey("0008,0090"))
                data.AddElementWithValueString(DcmTags.ReferringPhysiciansName, tags["0008,0090"].Substring(5));
            if (tags.ContainsKey("0008,1010"))
                data.AddElementWithValueString(DcmTags.StationName, tags["0008,1010"].Substring(5));
            if (tags.ContainsKey("0008,1030"))
                data.AddElementWithValueString(DcmTags.StudyDescription, tags["0008,1030"].Substring(5));
            if (tags.ContainsKey("0008,103e"))
                data.AddElementWithValueString(DcmTags.SeriesDescription, tags["0008,103e"].Substring(5));
            if (tags.ContainsKey("0008,1090"))
                data.AddElementWithValueString(DcmTags.ManufacturersModelName, tags["0008,1090"].Substring(5));

            if (tags.ContainsKey("0018,0010"))
                data.AddElementWithValueString(DcmTags.ContrastBolusAgent, tags["0018,0010"].Substring(5));
            if (tags.ContainsKey("0018,0015"))
                data.AddElementWithValueString(DcmTags.BodyPartExamined, tags["0018,0015"].Substring(5));
            if (tags.ContainsKey("0018,0050"))
                data.AddElementWithValueString(DcmTags.SliceThickness, tags["0018,0050"].Substring(5));
            if (tags.ContainsKey("0018,0060"))
                data.AddElementWithValueString(DcmTags.KVP, tags["0018,0060"].Substring(5));
            if (tags.ContainsKey("0018,0090"))
                data.AddElementWithValueString(DcmTags.DataCollectionDiameter, tags["0018,0090"].Substring(5));
            if (tags.ContainsKey("0018,1000"))
                data.AddElementWithValueString(DcmTags.DeviceSerialNumber, tags["0018,1000"].Substring(5));
            if (tags.ContainsKey("0018,1020"))
                data.AddElementWithValueString(DcmTags.SoftwareVersions, tags["0018,1020"].Substring(5));
            if (tags.ContainsKey("0018,1030"))
                data.AddElementWithValueString(DcmTags.ProtocolName, tags["0018,1030"].Substring(5));
            if (tags.ContainsKey("0018,1041"))
                data.AddElementWithValueString(DcmTags.ContrastBolusVolume, tags["0018,1041"].Substring(5));
            if (tags.ContainsKey("0018,1042"))
                data.AddElementWithValueString(DcmTags.ContrastBolusStartTime, tags["0018,1042"].Substring(5));
            if (tags.ContainsKey("0018,1043"))
                data.AddElementWithValueString(DcmTags.ContrastBolusStopTime, tags["0018,1043"].Substring(5));
            if (tags.ContainsKey("0018,1044"))
                data.AddElementWithValueString(DcmTags.ContrastBolusTotalDose, tags["0018,1044"].Substring(5));
            if (tags.ContainsKey("0018,1046"))
                data.AddElementWithValueString(DcmTags.ContrastFlowRate, tags["0018,1046"].Substring(5));
            if (tags.ContainsKey("0018,1047"))
                data.AddElementWithValueString(DcmTags.ContrastFlowDuration, tags["0018,1047"].Substring(5));
            if (tags.ContainsKey("0018,1049"))
                data.AddElementWithValueString(DcmTags.ContrastBolusIngredientConcentration, tags["0018,1049"].Substring(5));
            if (tags.ContainsKey("0018,1100"))
                data.AddElementWithValueString(DcmTags.ReconstructionDiameter, tags["0018,1100"].Substring(5));
            if (tags.ContainsKey("0018,1110"))
                data.AddElementWithValueString(DcmTags.DistanceSourceToDetector, tags["0018,1110"].Substring(5));
            if (tags.ContainsKey("0018,1111"))
                data.AddElementWithValueString(DcmTags.DistanceSourceToPatient, tags["0018,1111"].Substring(5));
            if (tags.ContainsKey("0018,1120"))
                data.AddElementWithValueString(DcmTags.GantryDetectorTilt, tags["0018,1120"].Substring(5));
            if (tags.ContainsKey("0018,1130"))
                data.AddElementWithValueString(DcmTags.TableHeight, tags["0018,1130"].Substring(5));
            if (tags.ContainsKey("0018,1140"))
                data.AddElementWithValueString(DcmTags.RotationDirection, tags["0018,1140"].Substring(5));
            if (tags.ContainsKey("0018,1150"))
                data.AddElementWithValueString(DcmTags.ExposureTime, tags["0018,1150"].Substring(5));
            if (tags.ContainsKey("0018,1151"))
                data.AddElementWithValueString(DcmTags.XRayTubeCurrent, tags["0018,1151"].Substring(5));
            if (tags.ContainsKey("0018,1152"))
                data.AddElementWithValueString(DcmTags.Exposure, tags["0018,1152"].Substring(5));
            if (tags.ContainsKey("0018,1160"))
                data.AddElementWithValueString(DcmTags.FilterType, tags["0018,1160"].Substring(5));
            if (tags.ContainsKey("0018,1170"))
                data.AddElementWithValueString(DcmTags.GeneratorPower, tags["0018,1170"].Substring(5));
            if (tags.ContainsKey("0018,1190"))
                data.AddElementWithValueString(DcmTags.FocalSpots, tags["0018,1190"].Substring(5));
            if (tags.ContainsKey("0018,1200"))
                data.AddElementWithValueString(DcmTags.DateOfLastCalibration, tags["0018,1200"].Substring(5));
            if (tags.ContainsKey("0018,1201"))
                data.AddElementWithValueString(DcmTags.TimeOfLastCalibration, tags["0018,1201"].Substring(5));
            if (tags.ContainsKey("0018,1210"))
                data.AddElementWithValueString(DcmTags.ConvolutionKernel, tags["0018,1210"].Substring(5));
            if (tags.ContainsKey("0018,5100"))
                data.AddElementWithValueString(DcmTags.PatientPosition, tags["0018,5100"].Substring(5));

            //if (tags.ContainsKey("0020,000D"))
            //    data.AddElementWithValueString(DcmTags.ContrastBolusStopTime, DateTime.Now.ToString());
            //if (tags.ContainsKey("0020,000E"))
            //    data.AddElementWithValueString(DcmTags.ContrastBolusStopTime, DateTime.Now.ToString());
            if (tags.ContainsKey("0020,0010"))
                data.AddElementWithValueString(DcmTags.StudyID, tags["0020,0010"].Substring(5));
            if (tags.ContainsKey("0020,0011"))
                data.AddElementWithValueString(DcmTags.SeriesNumber, tags["0020,0011"].Substring(5));
            if (tags.ContainsKey("0020,0012"))
                data.AddElementWithValueString(DcmTags.AccessionNumber, tags["0020,0012"].Substring(5));
            if (tags.ContainsKey("0020,0013"))
                data.AddElementWithValueString(DcmTags.InstanceNumber, tags["0020,0013"].Substring(5));
            if (tags.ContainsKey("0020,0032"))
                data.AddElementWithValueString(DcmTags.ImagePositionPatient, tags["0020,0032"].Substring(5));
            if (tags.ContainsKey("0020,0037"))
                data.AddElementWithValueString(DcmTags.ImageOrientationPatient, tags["0020,0037"].Substring(5));
            if (tags.ContainsKey("0020,0052"))
                data.AddElementWithValueString(DcmTags.FrameOfReferenceUID, tags["0020,0052"].Substring(5));
            if (tags.ContainsKey("0020,1040"))
                data.AddElementWithValueString(DcmTags.PositionReferenceIndicator, tags["0020,1040"].Substring(5));
            if (tags.ContainsKey("0020,1041"))
                data.AddElementWithValueString(DcmTags.SliceLocation, tags["0020,1041"].Substring(5));
            if (tags.ContainsKey("0020,4000"))
                data.AddElementWithValueString(DcmTags.ImageComments, tags["0020,4000"].Substring(5));

            


            //data.AddElementWithValueString(DcmTags.StudyTime, DateTime.Now.ToString());
            //data.AddElementWithValueString(DcmTags.AccessionNumber, "");
            //data.AddElementWithValueString(DcmTags.ReferringPhysiciansName, "");
            //data.AddElementWithValueString(DcmTags.StudyID, "1");
            //data.AddElementWithValueString(DcmTags.SeriesNumber, "1");
            //data.AddElementWithValueString(DcmTags.ModalitiesInStudy, "CT");//CR
            //data.AddElementWithValueString(DcmTags.Modality, "CT");//CR
            //data.AddElementWithValueString(DcmTags.NumberOfStudyRelatedInstances, "1");
            //data.AddElementWithValueString(DcmTags.NumberOfStudyRelatedSeries, "1");
            //data.AddElementWithValueString(DcmTags.NumberOfSeriesRelatedInstances, "1");
            //data.AddElementWithValueString(DcmTags.PatientOrientation, "HFS");//F/A
            //data.AddElementWithValueString(DcmTags.ImageLaterality, "U");
            if (tags.ContainsKey("0028,1050"))
                data.AddElementWithValueString(DcmTags.WindowCenter, "1113");
            if (tags.ContainsKey("0028,1051"))
                data.AddElementWithValueString(DcmTags.WindowWidth, "749");
            //data.AddElementWithValueString(DcmTags.WindowCenterWidthExplanation, "WINDOW1\\WINDOW2");
            data.AddElementWithValueString(DcmTags.PixelRepresentation, "0");
            data.AddElementWithValueString(DcmTags.RescaleIntercept, "0");//0
            data.AddElementWithValueString(DcmTags.RescaleSlope, "1");
            //data.AddElementWithValueString(DcmTags.RotationDirection, "CW");
            //ushort bitdepth = 2;未使用过

            DcmPixelData pixelData = new DcmPixelData(DcmTS.ImplicitVRLittleEndian);

            pixelData.PixelRepresentation = 0;//ok
            pixelData.ImageWidth = imgwidth;
            pixelData.ImageHeight = imgheight;

            pixelData.SamplesPerPixel = 1;//ok
            pixelData.HighBit = 15;//ok
            pixelData.BitsStored = 16;//ok
            pixelData.BitsAllocated = 16;//ok
            //pixelData.SamplesPerPixel = 1;
            //pixelData.HighBit = 7;
            //pixelData.BitsStored = 8;
            //pixelData.BitsAllocated = 8;
            pixelData.ImageType = "ORIGINAL\\PRIMARY\\AXIAL";
            pixelData.PhotometricInterpretation = "MONOCHROME2";//2 byte gray? //ok
          
            //pixelData.FragmentSize
            //pixelData.IsLossy = true;
            //pixelData.LossyCompressionMethod = "01";
            pixelData.PixelDataElement = DcmElement.Create(DcmTags.PixelData, DcmVR.OW); //OB: Other Byte, OW: Other Word

            //pixelData.AddFrame(bmpBytes);
            pixelData.AddFrame(greybytes);

            pixelData.UpdateDataset(data);
            DicomFileFormat ff = new DicomFileFormat(data);
            //string fileout = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "greyimg_test.dcm");
            ff.Save(fileout, Dicom.DicomWriteOptions.Default);//Default
            ff = null;
            return fileout;
        }
        private byte[] RGB2Gray(System.Drawing.Bitmap srcBitmap)
        {


            int wide = srcBitmap.Width;

            int height = srcBitmap.Height;

            byte[] pixdata = new byte[wide * height];

            System.Drawing.Rectangle rect = new System.Drawing.Rectangle(0, 0, wide, height);//将Bitmap锁定到系统内存中,获得BitmapData

            System.Drawing.Imaging.BitmapData srcBmData = srcBitmap.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format24bppRgb);//创建Bitmap

            //Bitmap dstBitmap = CreateGrayscaleImage(wide, height);//这个函数在后面有定义

            //BitmapData dstBmData = dstBitmap.LockBits(rect,ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);//位图中第一个像素数据的地址。它也可以看成是位图中的第一个扫描行

            System.IntPtr srcPtr = srcBmData.Scan0;

            //System.IntPtr dstPtr = dstBmData.Scan0;

            //将Bitmap对象的信息存放到byte数组中

            int src_bytes = srcBmData.Stride * height;

            byte[] srcValues = new byte[src_bytes];

            //int dst_bytes = dstBmData.Stride * height;

            //byte[] dstValues = new byte[dst_bytes];

            //复制GRB信息到byte数组

            System.Runtime.InteropServices.Marshal.Copy(srcPtr, srcValues, 0, src_bytes);

            //System.Runtime.InteropServices.Marshal.Copy(dstPtr, dstValues, 0, dst_bytes);

            //根据Y=0.299*R+0.114*G+0.587B,Y为亮度
            int index = 0;
            for (int i = 0; i < height; i++)

                for (int j = 0; j < wide; j++)
                {

                    //只处理每行中图像像素数据,舍弃未用空间

                    //注意位图结构中RGB按BGR的顺序存储

                    int k = 3 * j;

                    byte temp = (byte)(srcValues[i * srcBmData.Stride + k + 2] * .299

                         + srcValues[i * srcBmData.Stride + k + 1] * .587

                         + srcValues[i * srcBmData.Stride + k] * .114);

                    //dstValues[i * dstBmData.Stride + j] = temp;
                    pixdata[index] = temp;
                    index++;

                }

            //System.Runtime.InteropServices.Marshal.Copy(dstValues, 0, dstPtr, dst_bytes);

            //解锁位图

            srcBitmap.UnlockBits(srcBmData);

            //dstBitmap.UnlockBits(dstBmData);

            return pixdata;

        } 
    }
}
