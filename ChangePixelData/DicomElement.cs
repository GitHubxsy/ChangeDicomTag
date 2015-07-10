using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;

namespace FMI.CT.Review.Model
{
    public  class DicomElement
    {

         int rows;
         int cols;
        //定义变量（字段）
        public BitmapSource image;
        public List<short> ArrayRawpixData;
       // public short[] ArrayRawpixData ;
        public Dictionary<string, string> tags = new Dictionary<string, string>();

        private BinaryReader dicomFile;
        private bool isExplicitVR = true;//有无VR
        private UInt32 fileHeadLen;//文件头长度
        private long fileHeadOffset;//文件数据开始位置
        private long pixDataOffset;//像素数据开始位置
        private long pixDatalen;
        private bool isLitteEndian ;//是否小字节序（小端在前 、大端在前）
        string fileName = string.Empty;

        


        #region 构造函数
        public DicomElement()
        {
        }
        #endregion
        #region 获取默认图像
        /// <summary>
        /// 调用此函数，获取图像
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <returns>image(bitmapsource类型)</returns>
        public BitmapSource getImg_DefaultWindow(string fileName)
        {


            int windowWith = 0, windowCenter = 0 / 2;//在未设定时 窗宽窗位为0

            tagRead(fileName);//获取一些标签值 

            int rows = int.Parse(tags["0028,0010"].Substring(5));//行数
            int cols = int.Parse(tags["0028,0011"].Substring(5));//列数
            //int colors = int.Parse(tags["0028,0002"].Substring(5));//每个像素的通道数//颜色数 RGB为3 黑白为1
            //int dataLen = int.Parse(tags["0028,0100"].Substring(5));//bits allocated 每个像素需要读取的位数
            int validLen = int.Parse(tags["0028,0101"].Substring(5));//每个像素通道的位数
            //int hibit = int.Parse(tags["0028,0102"].Substring(5));//图像像素的最高位
            bool signed = int.Parse(tags["0028,0103"].Substring(5).Replace('\0', '0').Trim()) == 0 ? false : true;//是否是CT图

            float rescaleSlop = 1, rescaleinter = 0;
            if (tags.ContainsKey("0028,1052") && tags.ContainsKey("0028,1053"))//确定是否包含指定键，用以重新调节像素值
            {
                rescaleSlop = float.Parse(tags["0028,1053"].Substring(5));//权值
                rescaleinter = float.Parse(tags["0028,1052"].Substring(5));//偏置
            }
            #region 读取预设窗宽窗位
            if (windowWith == 0 && windowCenter == 0)
            {
                Regex r = new Regex(@"([0-9]+)+");
                if (tags.ContainsKey("0028,1051"))
                {
                    Match m = r.Match(tags["0028,1051"].Substring(5));

                    if (m.Success)
                        windowWith = int.Parse(m.Value);
                    else
                        windowWith = 1 << validLen;
                }
                else
                {
                    windowWith = 1 << validLen;
                }

                if (tags.ContainsKey("0028,1050"))
                {
                    Match m = r.Match(tags["0028,1050"].Substring(5));
                    if (m.Success)
                        windowCenter = int.Parse(m.Value);//窗位
                    else
                        windowCenter = windowWith / 2;
                }
                else
                {
                    windowCenter = windowWith / 2;
                }
            }

            #endregion

            long reads = 0;//图像数据已读取的字节数
            int height  = rows;
            int width = cols;
            int stride = height;
            int i = 0;
            double gray;
            int grayGDI;
            int grayStart = (windowCenter - windowWith / 2);
            int grayEnd = (windowCenter + windowWith / 2);

            byte[] pixData = new byte[height * width];//新建一个长度为512*512的整数数组。
            byte[] buffer = new byte[pixDatalen];
            MemoryStream mem = new MemoryStream((Int32)pixDatalen);
            dicomFile = new BinaryReader(File.OpenRead(fileName));
            dicomFile.BaseStream.Seek(pixDataOffset, SeekOrigin.Begin);
            dicomFile.Read(buffer, 0, (Int32)pixDatalen);
            dicomFile.Close();
            mem.Write(buffer, 0, (Int32)pixDatalen);
            //mem.Seek(0, SeekOrigin.Begin);    //设置当前流正在读取的位置 为开始位置即从0开始
            mem.Position = 0;//设置流的当前的位置
            while (reads < pixDatalen)
            {
                byte[] piels = new byte[2];//新建一个字节数组存放每个像素。长度为每个像素的长度。
                mem.Read(piels, 0, 2);
                if (isLitteEndian == false)
                    Array.Reverse(piels, 0, 2);

                if (signed == false)//如果不是CT图
                    gray = BitConverter.ToUInt16(piels, 0);
                else
                    gray = BitConverter.ToInt16(piels, 0);//CT图一般值在-1000至+3096.

                if ((rescaleSlop != 1.0f) || (rescaleinter != 0.0f))//如果rescaleSlop不等于1.0，或者rescaleinter不等于0，则
                {
                    float fValue = ((float)gray * rescaleSlop + rescaleinter);
                    gray = (short)fValue;
                }
                ArrayRawpixData[i] = (short)gray;

                if (gray < grayStart)
                    grayGDI = 0;
                else if (gray > grayEnd)
                    grayGDI = 255;
                else
                {
                    grayGDI = (int)((gray - grayStart) * 255 / windowWith);
                    if (grayGDI > 255)
                        grayGDI = 255;
                    else if (grayGDI < 0)
                        grayGDI = 0;
                }
                pixData[i] = (byte)grayGDI;
                reads += piels.Length;
                i++;
            }
            mem.Close();
            return image = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixData, stride);
        }
        #endregion
        #region 自动调整窗宽窗位
        /// <summary>
        /// 根据不同的窗宽，窗位，获取图像
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// <param name="windowWith">窗宽</param>
        /// <param name="windowCenter">窗位</param>
        /// <returns>BitmapSource类型</returns>
        public BitmapSource getImg_Auto(string fileName, int windowWith, int windowCenter)
        {
            //if (fileName == string.Empty)
            //    return false;
           

            tagRead(fileName);//获取一些标签值

            

            int rows = int.Parse(tags["0028,0010"].Substring(5));//行数
            int cols = int.Parse(tags["0028,0011"].Substring(5));//列数
            //int colors = int.Parse(tags["0028,0002"].Substring(5));//每个像素的通道数//颜色数 RGB为3 黑白为1
            //int dataLen = int.Parse(tags["0028,0100"].Substring(5));//bits allocated 每个像素需要读取的位数
            int validLen = int.Parse(tags["0028,0101"].Substring(5));//每个像素通道的位数
            //int hibit = int.Parse(tags["0028,0102"].Substring(5));//图像像素的最高位
            bool signed = int.Parse(tags["0028,0103"].Substring(5).Replace('\0', '0').Trim()) == 0 ? false : true;//是否是CT图

            float rescaleSlop = 1, rescaleinter = 0;
            if (tags.ContainsKey("0028,1052") && tags.ContainsKey("0028,1053"))//确定是否包含指定键，用以重新调节像素值
            {
                rescaleSlop = float.Parse(tags["0028,1053"].Substring(5));//权值
                rescaleinter = float.Parse(tags["0028,1052"].Substring(5));//偏置
            }

            long reads = 0;//图像数据已读取的字节数

            int width = rows;
            int height = cols;
            int stride = width;
            int i = 0;
            double gray;
            int grayGDI;
            int grayStart = (windowCenter - windowWith / 2);
            int grayEnd = (windowCenter + windowWith / 2);

            byte[] pixData = new byte[height * width];//新建一个长度为512*512的整数数组。
            byte[] buffer = new byte[pixDatalen];
            MemoryStream mem = new MemoryStream((Int32)pixDatalen);
            dicomFile = new BinaryReader(File.OpenRead(fileName));
            dicomFile.BaseStream.Seek(pixDataOffset, SeekOrigin.Begin);
            dicomFile.Read(buffer, 0, (Int32)pixDatalen);
            dicomFile.Close();
            mem.Write(buffer, 0, (Int32)pixDatalen);
            //mem.Seek(0, SeekOrigin.Begin);    //设置当前流正在读取的位置 为开始位置即从0开始
            mem.Position = 0;
            //byte[] buffer = new byte[fileName.Length];
            //MemoryStream mem = new MemoryStream((Int32)fileName.Length);
            //dicomFile = new BinaryReader(File.OpenRead(fileName));
            ////dicomFile.BaseStream.Seek(pixDataOffset, SeekOrigin.Begin);
            //dicomFile.Read(buffer, 0, (Int32)fileName.Length);
            
            //mem.Write(buffer, 0, (Int32)fileName.Length);
            ////mem.Seek(0, SeekOrigin.Begin);    //设置当前流正在读取的位置 为开始位置即从0开始
            //mem.Position = pixDataOffset;
            //dicomFile.Close();
            while (reads < pixDatalen)
            {
                byte[] piels = new byte[2];//新建一个字节数组存放每个像素。长度为每个像素的长度。
                mem.Read(piels, 0, 2);
                if (isLitteEndian == false)
                    Array.Reverse(piels, 0, 2);

                if (signed == false)//如果不是CT图
                    gray = BitConverter.ToUInt16(piels, 0);
                else
                    gray = BitConverter.ToInt16(piels, 0);//CT图一般值在-1000至+3096.

                if ((rescaleSlop != 1.0f) || (rescaleinter != 0.0f))//如果rescaleSlop不等于1.0，或者rescaleinter不等于0，则
                {
                    float fValue = ((float)gray * rescaleSlop + rescaleinter);
                    gray = (short)fValue;
                }

                if (gray < grayStart)
                    grayGDI = 0;
                else if (gray > grayEnd)
                    grayGDI = 255;
                else
                {
                    grayGDI = (int)((gray - grayStart) * 255 / windowWith);
                    if (grayGDI > 255)
                        grayGDI = 255;
                    else if (grayGDI < 0)
                        grayGDI = 0;
                }
                pixData[i] = (byte)grayGDI;
                reads += piels.Length;
                i++;
            }
            mem.Close();//
            return  image =BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixData, stride);
            
        }
        #endregion
        #region 改进版的自动调整窗宽窗位
        public BitmapSource getImg_Auto_improve(List<short> ArrayRawpixData, int windowWith, int windowCenter)
        {
            //int width = 512;
            //int height = 512;
            int stride = cols;
            short gray;
            int grayGDI;
            int grayStart = (windowCenter - windowWith / 2);
            int grayEnd = (windowCenter + windowWith / 2);
            byte[] Arraypixdata = new byte[cols* rows];

            for (int i = 0; i < ArrayRawpixData.Count; i++)
            {
                gray = ArrayRawpixData[i];
                if (gray < grayStart)
                    grayGDI = 0;
                else if (gray > grayEnd)
                    grayGDI = 255;
                else
                {
                    if (windowWith == 0)
                        windowWith = 1;
                    grayGDI = (int)((gray - grayStart) * 255 / windowWith);
                    if (grayGDI > 255)
                        grayGDI = 255;
                    else if (grayGDI < 0)
                        grayGDI = 0;
                }
                Arraypixdata[i] = (byte)grayGDI;
            }//
            image =BitmapSource.Create(cols,rows,  96, 96, PixelFormats.Gray8, null, Arraypixdata, stride);
            //Arraypixdata=null;
            return image;

        }
        #endregion



        
        #region 判断文件时候为DICOM格式
        private BitmapSource Judge(string fileName)
        {

            dicomFile = new BinaryReader(File.OpenRead(fileName));
            //跳过128字节导言部分
            dicomFile.BaseStream.Seek(128, SeekOrigin.Begin);

            if (new string(dicomFile.ReadChars(4)) != "DICM")
            {
                Console.Write(fileName + "：不是DICOM格式");
                //System.Windows.MessageBox.Show();
            }
            dicomFile.Close();
            return null; 
        }
        #endregion
        #region 读取标签
        private void tagRead(string fileName)
        {
            bool enDir = false;
            int leve = 0;
            StringBuilder folderData = new StringBuilder();//定义一个字符串
            string folderTag = string.Empty;// 定义一个字符串并赋值为空
            dicomFile = new BinaryReader(File.OpenRead(fileName));

            //跳过128字节导言部分
            dicomFile.BaseStream.Seek(128, SeekOrigin.Begin);
            while (dicomFile.BaseStream.Position + 6 < dicomFile.BaseStream.Length)//直到全部读取数据
            {
                //读取tag
                string tag = dicomFile.ReadUInt16().ToString("x4") + "," +
                dicomFile.ReadUInt16().ToString("x4");//标签格式（组号，元素号）

                string VR = string.Empty;
                UInt32 Len = 0;
                //读取VR跟Len
                //对OB OW SQ 要做特殊处理 先置两个字节0 然后4字节值长度
                //特殊情况：不同的数据类型，len占的字节数也会不一样。
                if (tag.Substring(0, 4) == "0002")//标签中的组号为0002
                {
                    VR = new string(dicomFile.ReadChars(2));//读取两个字节赋值VR

                    if (VR == "OB" || VR == "OW" || VR == "SQ" || VR == "OF" || VR == "UT" || VR == "UN")//数据类型是0B或OW或...
                    {
                        dicomFile.BaseStream.Seek(2, SeekOrigin.Current);//当前流中跳过2个字节。
                        Len = dicomFile.ReadUInt32();//读取4个字节，赋值长度Len
                    }
                    else
                        Len = dicomFile.ReadUInt16();//读取2个字节，赋值长度Len
                }
                else if (tag == "fffe,e000" || tag == "fffe,e00d" || tag == "fffe,e0dd")//如果标签为"fffe,e000"，"fffe,e00d"，"fffe,e0dd"。
                {
                    VR = "**";
                    Len = dicomFile.ReadUInt32();//读取4个字节，赋值长度Len
                }
                else if (isExplicitVR == true)//有VR的情况
                {
                    VR = new string(dicomFile.ReadChars(2));

                    if (VR == "OB" || VR == "OW" || VR == "SQ" || VR == "OF" || VR == "UT" || VR == "UN")
                    {
                        dicomFile.BaseStream.Seek(2, SeekOrigin.Current);
                        Len = dicomFile.ReadUInt32();//读取4个字节，赋值长度Len
                    }
                    else

                        Len = dicomFile.ReadUInt16();//读取2个字节，赋值长度Len
                }
                else if (isExplicitVR == false)//无VR的情况
                {
                    VR = getVR(tag);//无显示VR时根据tag一个一个去找。
                    Len = dicomFile.ReadUInt32();
                }


                //判断是否应该读取VF 以何种方式读取VF
                //这些都是在读取VF一步被阻断的情况
                byte[] VF = { 0x00 };

                if (tag == "7fe0,0010")//图像像素数据开始点
                {
                    pixDatalen = Len;
                    pixDataOffset = dicomFile.BaseStream.Position;
                    dicomFile.BaseStream.Seek(Len, SeekOrigin.Current);

                    VR = "UL";
                    VF = BitConverter.GetBytes(Len);
                }


                else if ((VR == "SQ" && Len == UInt32.MaxValue) || (tag == "fffe,e000" && Len == UInt32.MaxValue))
                {
                    if (enDir == false)
                    {
                        enDir = true;
                        folderData.Remove(0, folderData.Length);
                        folderTag = tag;
                    }
                    else
                    {
                        leve++;//VF不赋值
                    }
                }
                else if ((tag == "fffe,e00d" && Len == UInt32.MinValue) || (tag == "fffe,e0dd" && Len == UInt32.MinValue))
                {
                    if (enDir == true)
                    {
                        enDir = false;
                    }
                    else
                    {
                        leve--;
                    }
                }
                else
                    VF = dicomFile.ReadBytes((int)Len);



                string VFStr;

                VFStr = getVF(VR, VF);//将VF转换成对应的信息

                //针对特殊的tag的值的处理
                //特别针对文件头信息处理
                if (tag == "0002,0000")    //Group length
                {
                    fileHeadLen = Len;
                    fileHeadOffset = dicomFile.BaseStream.Position;
                }
                else if (tag == "0002,0010")//传输语法 关系到后面的数据读取
                {
                    switch (VFStr)
                    {
                        case "1.2.840.10008.1.2.1\0"://显式little
                            isLitteEndian = true;
                            isExplicitVR = true;
                            break;
                        case "1.2.840.10008.1.2.2\0"://显式big
                            isLitteEndian = false;
                            isExplicitVR = true;
                            break;
                        case "1.2.840.10008.1.2\0"://隐式little
                            isLitteEndian = true;
                            isExplicitVR = false;
                            break;
                        default:
                            break;
                    }
                }
                for (int i = 1; i <= leve; i++)
                    tag = "--" + tag;
                //数据搜集代码
                if ((VR == "SQ" && Len == UInt32.MaxValue) || (tag == "fffe,e000" && Len == UInt32.MaxValue) || leve > 0)
                {
                    folderData.AppendLine(tag + "(" + VR + ")：" + VFStr);
                }
                else if (((tag == "fffe,e00d" && Len == UInt32.MinValue) || (tag == "fffe,e0dd" && Len == UInt32.MinValue)) && leve == 0)
                {
                    folderData.AppendLine(tag + "(" + VR + ")：" + VFStr);
                    tags.Add(folderTag + "SQ", folderData.ToString());
                }
                else
                    tags.Add(tag, "(" + VR + "):" + VFStr);
            }
            //dicomFile.Close();
        }
        #endregion
        #region 根据标签获取VR
        private string getVR(string tag)
        {
            switch (tag)
            {
                case "0002,0000"://文件元信息长度
                    return "UL";
                //  break;
                case "0002,0010"://传输语法
                    return "UI";
                //  break;
                case "0002,0013"://文件生成程序的标题
                    return "SH";
                // break;
                case "0008,0005"://文本编码
                    return "CS";
                //  break;
                case "0008,0008":
                    return "CS";
                // break;
                case "0008,1032"://成像时间
                    return "SQ";
                // break;
                case "0008,1111":
                    return "SQ";
                //break;
                case "0008,0020"://检查日期
                    return "DA";
                // break;
                case "0008,0060"://成像仪器
                    return "CS";
                //  break;
                case "0008,0070"://成像仪厂商
                    return "LO";
                //  break;
                case "0008,0080":
                    return "LO";
                // break;
                case "0010,0010"://病人姓名
                    return "PN";
                // break;
                case "0010,0020"://病人id
                    return "LO";
                // break;
                case "0010,0030"://病人生日
                    return "DA";
                // break;
                case "0018,0060"://电压
                    return "DS";
                // break;
                case "0018,1030"://协议名
                    return "LO";
                //break;
                case "0018,1151":
                    return "IS";
                //  break;
                case "0020,0010"://检查ID
                    return "SH";
                //  break;
                case "0020,0011"://序列
                    return "IS";
                //break;
                case "0020,0012"://成像编号
                    return "IS";
                //  break;
                case "0020,0013"://影像编号
                    return "IS";
                //  break;
                case "0028,0002"://像素采样1为灰度3为彩色
                    return "US";
                // break;
                case "0028,0004"://图像模式MONOCHROME2为灰度
                    return "CS";
                // break;
                case "0028,0010"://row高
                    return "US";
                //  break;
                case "0028,0011"://col宽
                    return "US";
                // break;
                case "0028,0100"://单个采样数据长度
                    return "US";
                // break;
                case "0028,0101"://实际长度
                    return "US";
                //  break;
                case "0028,0102"://采样最大值
                    return "US";
                // break;
                case "0028,1050"://窗位
                    return "DS";
                //break;
                case "0028,1051"://窗宽
                    return "DS";
                // break;
                case "0028,1052":
                    return "DS";
                //  break;
                case "0028,1053":
                    return "DS";
                // break;
                case "0040,0008"://文件夹标签
                    return "SQ";
                // break;
                case "0040,0260"://文件夹标签
                    return "SQ";
                //  break;
                case "0040,0275"://文件夹标签
                    return "SQ";
                // break;
                case "7fe0,0010"://像素数据开始处
                    return "OW";
                // break;
                default:
                    return "UN";
                // break;
            }
        }
        #endregion
        #region 根据VR，获得VF
        private string getVF(string VR, byte[] VF)
        {
            string VFStr = string.Empty;
            switch (VR)
            {
                case "SS":
                    VFStr = BitConverter.ToInt16(VF, 0).ToString();
                    break;
                case "US":
                    VFStr = BitConverter.ToUInt16(VF, 0).ToString();

                    break;
                case "SL":
                    VFStr = BitConverter.ToInt32(VF, 0).ToString();

                    break;
                case "UL":
                    VFStr = BitConverter.ToUInt32(VF, 0).ToString();

                    break;
                case "AT":
                    VFStr = BitConverter.ToUInt16(VF, 0).ToString();

                    break;
                case "FL":
                    VFStr = BitConverter.ToSingle(VF, 0).ToString();

                    break;
                case "FD":
                    if (VF.Length!=0)
                    {
                        VFStr = BitConverter.ToDouble(VF, 0).ToString(); 
                    }

                    break;
                case "OB":
                    VFStr = BitConverter.ToString(VF, 0);
                    break;
                case "OW":
                    VFStr = BitConverter.ToString(VF, 0);
                    break;
                case "SQ":
                    VFStr = BitConverter.ToString(VF, 0);
                    break;
                case "OF":
                    VFStr = BitConverter.ToString(VF, 0);
                    break;
                case "UT":
                    VFStr = BitConverter.ToString(VF, 0);
                    break;
                case "UN":
                    VFStr = Encoding.Default.GetString(VF);
                    break;
                default:
                    VFStr = Encoding.Default.GetString(VF);
                    break;
            }
            return VFStr;
        }
        #endregion
        #region 字典初始化
        //调用方式
        //handler.DictionaryInitlize();
        //string str=handler.dict["0000,0001"];
        //public void DictionaryInitlize()
        //{
        //    string lines = YuMeng.Properties.Resources.dicom; //string dicomdict = WpfApplication1.Properties.Resources.dicom;
        //    string[] slines = lines.Split(new char[2] { '\n', '\t' });
        //    for (int i = 0; i < slines.Length / 3; i++)
        //    {
        //        dict.Add(slines[3 * i] + ',' + slines[3 * i + 1], slines[3 * i + 2]);
        //    }
        //}
        #endregion



        #region 用于打开按钮
        public  byte[] pixData;
        public byte[] buffer;
        public BitmapSource getImg_DefaultWindow_openfile(string fileName)
        {
            dicomFile = new BinaryReader(File.OpenRead(fileName));
            //跳过128字节导言部分
            dicomFile.BaseStream.Seek(128, SeekOrigin.Begin);

            if (new string(dicomFile.ReadChars(4)) != "DICM")
            {
                throw new Exception(fileName + "：不是DICOM格式");
            }

            #region
            bool enDir = false;
            int leve = 0;
            StringBuilder folderData = new StringBuilder();//定义一个字符串
            string folderTag = string.Empty;// 定义一个字符串并赋值为空
            while (dicomFile.BaseStream.Position + 6 < dicomFile.BaseStream.Length)//直到全部读取数据
            {
                //读取tag
                string tag = dicomFile.ReadUInt16().ToString("x4") + "," +
                dicomFile.ReadUInt16().ToString("x4");//标签格式（组号，元素号）

                string VR = string.Empty;
                UInt32 Len = 0;
                //读取VR跟Len
                //对OB OW SQ 要做特殊处理 先置两个字节0 然后4字节值长度
                //特殊情况：不同的数据类型，len占的字节数也会不一样。
                if (tag.Substring(0, 4) == "0002")//标签中的组号为0002
                {
                    VR = new string(dicomFile.ReadChars(2));//读取两个字节赋值VR

                    if (VR == "OB" || VR == "OW" || VR == "SQ" || VR == "OF" || VR == "UT" || VR == "UN")//数据类型是0B或OW或...
                    {
                        dicomFile.BaseStream.Seek(2, SeekOrigin.Current);//当前流中跳过2个字节。
                        Len = dicomFile.ReadUInt32();//读取4个字节，赋值长度Len
                    }
                    else
                        Len = dicomFile.ReadUInt16();//读取2个字节，赋值长度Len
                }
                else if (tag == "fffe,e000" || tag == "fffe,e00d" || tag == "fffe,e0dd")//如果标签为"fffe,e000"，"fffe,e00d"，"fffe,e0dd"。
                {
                    VR = "**";
                    Len = dicomFile.ReadUInt32();//读取4个字节，赋值长度Len
                }
                else if (isExplicitVR == true)//有VR的情况
                {
                    VR = new string(dicomFile.ReadChars(2));

                    if (VR == "OB" || VR == "OW" || VR == "SQ" || VR == "OF" || VR == "UT" || VR == "UN")
                    {
                        dicomFile.BaseStream.Seek(2, SeekOrigin.Current);
                        Len = dicomFile.ReadUInt32();//读取4个字节，赋值长度Len
                    }
                    else

                        Len = dicomFile.ReadUInt16();//读取2个字节，赋值长度Len
                }
                else if (isExplicitVR == false)//无VR的情况
                {
                    VR = getVR(tag);//无显示VR时根据tag一个一个去找。
                    Len = dicomFile.ReadUInt32();
                }


                //判断是否应该读取VF 以何种方式读取VF
                //这些都是在读取VF一步被阻断的情况
                byte[] VF = { 0x00 };

                if (tag == "7fe0,0010")//图像像素数据开始点
                {
                    pixDatalen = Len;//图像的像素数据的长度
                    pixDataOffset = dicomFile.BaseStream.Position;//偏移量
                    dicomFile.BaseStream.Seek(Len, SeekOrigin.Current);

                    VR = "UL";
                    VF = BitConverter.GetBytes(Len);
                }


                else if ((VR == "SQ" && Len == UInt32.MaxValue) || (tag == "fffe,e000" && Len == UInt32.MaxValue || (VR == "UN" && Len >= Int32.MaxValue)))
                {
                    if (enDir == false)
                    {
                        enDir = true;
                        folderData.Remove(0, folderData.Length);
                        folderTag = tag;
                    }
                    else
                    {
                        leve++;//VF不赋值
                    }
                }
                else if ((tag == "fffe,e00d" && Len == UInt32.MinValue) || (tag == "fffe,e0dd" && Len == UInt32.MinValue))
                {
                    if (enDir == true)
                    {
                        enDir = false;
                    }
                    else
                    {
                        leve--;
                    }
                }
                else
                    VF = dicomFile.ReadBytes((int)Len);



                string VFStr;

                VFStr = getVF(VR, VF);//将VF转换成对应的信息

                //针对特殊的tag的值的处理
                //特别针对文件头信息处理
                if (tag == "0002,0000")    //Group length
                {
                    fileHeadLen = Len;
                    fileHeadOffset = dicomFile.BaseStream.Position;
                }
                else if (tag == "0002,0010")//传输语法 关系到后面的数据读取
                {
                    switch (VFStr)
                    {
                        case "1.2.840.10008.1.2.1\0"://显式little
                            isLitteEndian = true;
                            isExplicitVR = true;
                            break;
                        case "1.2.840.10008.1.2.2\0"://显式big
                            isLitteEndian = false;
                            isExplicitVR = true;
                            break;
                        case "1.2.840.10008.1.2\0"://隐式little
                            isLitteEndian = true;
                            isExplicitVR = false;
                            break;
                        default:
                            break;
                    }
                }
                for (int i = 1; i <= leve; i++)
                    tag = "--" + tag;
                //数据搜集代码
                if ((VR == "SQ" && Len == UInt32.MaxValue) || (tag == "fffe,e000" && Len == UInt32.MaxValue) || leve > 0)
                {
                    folderData.AppendLine(tag + "(" + VR + ")：" + VFStr);
                }
                else if (((tag == "fffe,e00d" && Len == UInt32.MinValue) || (tag == "fffe,e0dd" && Len == UInt32.MinValue)) && leve == 0)
                {
                    folderData.AppendLine(tag + "(" + VR + ")：" + VFStr);
                    tags.Add(folderTag + "SQ", folderData.ToString());
                }
                else
                    tags.Add(tag, "(" + VR + "):" + VFStr);
            }
            //dicomFile.Close();
            #endregion


            rows = int.Parse(tags["0028,0010"].Substring(5));//行数
            cols = int.Parse(tags["0028,0011"].Substring(5));//列数
            //int colors = int.Parse(tags["0028,0002"].Substring(5));//每个像素的通道数//颜色数 RGB为3 黑白为1
            //int dataLen = int.Parse(tags["0028,0100"].Substring(5));//bits allocated 每个像素需要读取的位数
            int validLen = int.Parse(tags["0028,0101"].Substring(5));//每个像素通道的位数
            //int hibit = int.Parse(tags["0028,0102"].Substring(5));//图像像素的最高位
            bool signed = int.Parse(tags["0028,0103"].Substring(5).Replace('\0', '0').Trim()) == 0 ? false : true;//是否是CT图

            float rescaleSlop = 1, rescaleinter = 0;
            if (tags.ContainsKey("0028,1052") && tags.ContainsKey("0028,1053"))//确定是否包含指定键，用以重新调节像素值
            {
                rescaleSlop = float.Parse(tags["0028,1053"].Substring(5));//权值
                rescaleinter = float.Parse(tags["0028,1052"].Substring(5));//偏置
            }
            #region 读取预设窗宽窗位
            int windowwith = 0, windowcenter = 0 / 2;//在未设定时 窗宽窗位为0

            if (windowwith == 0 && windowcenter == 0)
            {
                Regex r = new Regex(@"([0-9]+)+");
                if (tags.ContainsKey("0028,1051"))
                {
                    Match m = r.Match(tags["0028,1051"].Substring(5));

                    if (m.Success)
                        windowwith = int.Parse(m.Value);
                    else
                        windowwith = 1 << validLen;
                }
                else
                {
                    windowwith = 1 << validLen;
                }

                if (tags.ContainsKey("0028,1050"))
                {
                    Match m = r.Match(tags["0028,1050"].Substring(5));
                    if (m.Success)
                        windowcenter = int.Parse(m.Value);//窗位
                    else
                        windowcenter = windowwith / 2;
                }
                else
                {
                    windowcenter = windowwith / 2;
                }
            }

            #endregion

            long reads = 0;//图像数据已读取的字节数
            int height = rows;
            int width  = cols;
            int stride = width;
            int index = 0;
            double gray;
            int grayGDI;
            int grayStart = (windowcenter - windowwith / 2);
            int grayEnd = (windowcenter + windowwith / 2);

            pixData = new byte[height * width];//新建一个长度为512*512的整数数组。
            buffer = new byte[pixDatalen];  
            dicomFile.BaseStream.Seek(pixDataOffset, SeekOrigin.Begin);
            dicomFile.Read(buffer, 0, (Int32)pixDatalen);
            dicomFile.Close();
            dicomFile.Dispose();
            byte[] piels;
            float fValue;
            
            ArrayRawpixData = new List<short>();
            
            using (MemoryStream mem = new MemoryStream((Int32)pixDatalen))
            {
                mem.Write(buffer, 0, (Int32)pixDatalen);
                //mem.Seek(0, SeekOrigin.Begin);    //设置当前流正在读取的位置 为开始位置即从0开始
                mem.Position = 0;//设置流的当前的位置
                //ArrayRawpixData = new short[height * width];
                
               
                while (reads < pixDatalen)
                {
                    piels = new byte[2];//新建一个字节数组存放每个像素。长度为每个像素的长度。
                    mem.Read(piels, 0, 2);

                    if (isLitteEndian == false)
                        Array.Reverse(piels, 0, 2);

                    if (signed == false)//如果不是CT图
                        gray = BitConverter.ToUInt16(piels, 0);
                    else
                        gray = BitConverter.ToInt16(piels, 0);//CT图一般值在-1000至+3096.

                    if ((rescaleSlop != 1.0f) || (rescaleinter != 0.0f))//如果rescaleSlop不等于1.0，或者rescaleinter不等于0，则
                    {
                        fValue = ((float)gray * rescaleSlop + rescaleinter);
                        gray = (short)fValue;
                    }

                     //ArrayRawpixData[index] = (short)gray;
                   ArrayRawpixData.Add((short)gray);
                   if (gray < grayStart)
                       grayGDI = 0;
                   else if (gray > grayEnd)
                       grayGDI = 255;
                   else
                   {
                       grayGDI = (int)((gray - grayStart) * 255 / windowwith);
                       if (grayGDI > 255)
                           grayGDI = 255;
                       else if (grayGDI < 0)
                           grayGDI = 0;
                   }
                   pixData[index] = (byte)grayGDI;
                    reads += piels.Length;
                    index++;
                }
                mem.Close();
                mem.Dispose();
                
            }
            
            //dicomFile = new BinaryReader(File.OpenRead(fileName));
            image = BitmapSource.Create(width, height, 96, 96, PixelFormats.Gray8, null, pixData, stride);
            //pixData=null;
            //buffer = null ;

            return image;
        }
        
        #endregion

        

    }
}