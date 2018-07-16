using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DropDealer
{
    class Program
    {
        private static bool IsUTF8Bytes(byte[] data)
        {
            int charByteCounter = 1;  //计算当前正分析的字符应还有的字节数
            byte curByte; //当前分析的字节.
            for (int i = 0; i < data.Length; i++)
            {
                curByte = data[i];
                if (charByteCounter == 1)
                {
                    if (curByte >= 0x80)
                    {
                        //判断当前
                        while (((curByte <<= 1) & 0x80) != 0)
                        {
                            charByteCounter++;
                        }
                        //标记位首位若为非0 则至少以2个1开始 如:110XXXXX...........1111110X　
                        if (charByteCounter == 1 || charByteCounter > 6)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    //若是UTF-8 此时第一位必须为1
                    if ((curByte & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    charByteCounter--;
                }
            }
            if (charByteCounter > 1)
            {
                throw new Exception("非预期的byte格式");
            }
            return true;
        }

        public static System.Text.Encoding GetType(System.IO.FileStream fs)
        {
            byte[] Unicode = new byte[] { 0xFF, 0xFE, 0x41 };
            byte[] UnicodeBIG = new byte[] { 0xFE, 0xFF, 0x00 };
            byte[] UTF8 = new byte[] { 0xEF, 0xBB, 0xBF }; //带BOM
            System.Text.Encoding reVal = System.Text.Encoding.Default;

            System.IO.BinaryReader r = new System.IO.BinaryReader(fs, System.Text.Encoding.Default);
            int i;
            int.TryParse(fs.Length.ToString(), out i);
            byte[] ss = r.ReadBytes(i);
            if (IsUTF8Bytes(ss) || (ss[0] == 0xEF && ss[1] == 0xBB && ss[2] == 0xBF))
            {
                reVal = System.Text.Encoding.UTF8;
            }
            else if (ss[0] == 0xFE && ss[1] == 0xFF && ss[2] == 0x00)
            {
                reVal = System.Text.Encoding.BigEndianUnicode;
            }
            else if (ss[0] == 0xFF && ss[1] == 0xFE && ss[2] == 0x41)
            {
                reVal = System.Text.Encoding.Unicode;
            }
            r.Close();
            return reVal;
        }

        public static DataTable OpenCSV(string filePath)//从csv读取数据返回table
        {
             //Encoding.ASCII;//
            DataTable dt = new DataTable();
            System.IO.FileStream fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open,
                System.IO.FileAccess.Read);
            System.Text.Encoding encoding = GetType(fs);
            fs.Close();
            fs = new System.IO.FileStream(filePath, System.IO.FileMode.Open,
                System.IO.FileAccess.Read);
            System.IO.StreamReader sr = new System.IO.StreamReader(fs, encoding);

            //记录每次读取的一行记录
            string strLine = "";
            //记录每行记录中的各字段内容
            string[] aryLine = null;
            string[] tableHead = null;
            //标示列数
            int columnCount = 0;
            //标示是否是读取的第一行
            bool IsFirst = true;
            //逐行读取CSV中的数据
            while ((strLine = sr.ReadLine()) != null)
            {
                if (IsFirst == true)
                {
                    tableHead = strLine.Split(',');
                    IsFirst = false;
                    columnCount = tableHead.Length;
                    //创建列
                    for (int i = 0; i < columnCount; i++)
                    {
                        DataColumn dc = new DataColumn(tableHead[i]);
                        dt.Columns.Add(dc);
                    }
                }
                else
                {
                    aryLine = strLine.Split(',');
                    DataRow dr = dt.NewRow();
                    for (int j = 0; j < columnCount; j++)
                    {
                        dr[j] = aryLine[j];
                    }
                    dt.Rows.Add(dr);
                }
            }
            if (aryLine != null && aryLine.Length > 0)
            {
                dt.DefaultView.Sort = tableHead[0] + " " + "asc";
            }

            sr.Close();
            fs.Close();
            return dt;
        }

        public static int searchID(List<KeyValuePair<int, int>> reflectList, int ID, bool original = true)
        {
            for (int i = 0; i < reflectList.Count; i++)
            {
                if ((original ? reflectList[i].Key : reflectList[i].Value) == ID)
                    return i;
            }
            return -1;
        }

        static void Main(string[] args)
        {
            DataTable mobs = OpenCSV("./mobs.csv");
            DataTable items = OpenCSV("./items.csv");
            DataTable md = OpenCSV("./monsterdrops.csv");

            List<KeyValuePair<int, int>> itemIDReflect = new List<KeyValuePair<int, int>>();
            List<KeyValuePair<int, int>> mobIDReflect = new List<KeyValuePair<int, int>>();

            for (int i = 0; i < items.Rows.Count; i++)
            {
                itemIDReflect.Add(new KeyValuePair<int, int>(int.Parse((string)items.Rows[i][1]), i + 170000000));
            }
            for (int i = 0; i < mobs.Rows.Count; i++)
            {
                mobIDReflect.Add(new KeyValuePair<int, int>(int.Parse((string)mobs.Rows[i][1]), i + 390000000));
            }

            FileStream fs;
            StreamWriter sw;

            fs = new FileStream("./output_mobs.csv", FileMode.OpenOrCreate);
            sw = new StreamWriter(fs, Encoding.UTF8);
            sw.WriteLine("id,name,lv,exp");
            for (int i = 0; i < mobs.Rows.Count; i++)
            {
                sw.WriteLine(mobIDReflect[i].Value + "," + mobs.Rows[i][2] + "," + mobs.Rows[i][4] + "," + mobs.Rows[i][14]);
                string mob_id_file = "./img/mobIcon/" + String.Format("{0:D7}", int.Parse((string)mobs.Rows[i][1])) + ".png";
                if (File.Exists(mob_id_file))
                {
                    File.Copy(mob_id_file, "./output_img/mob/" + mobIDReflect[i].Value + ".png", true);
                }
                if (i % (mobs.Rows.Count / 10) == 0)
                    Console.WriteLine("Mobs Copy " + i / (mobs.Rows.Count / 10) + "0%");
            }
            sw.Close();
            fs.Close();

            fs = new FileStream("./output_items.csv", FileMode.OpenOrCreate);
            sw = new StreamWriter(fs, Encoding.UTF8);
            sw.WriteLine("id,name,lv");
            for (int i = 0; i < items.Rows.Count; i++)
            {
                sw.WriteLine(itemIDReflect[i].Value + "," + items.Rows[i][2] + "," + items.Rows[i][3]);
                string item_id_file = "./img/itemIcon/0"+items.Rows[i][1].ToString() + ".png";
                if (File.Exists(item_id_file))
                {
                    File.Copy(item_id_file, "./output_img/item/" + itemIDReflect[i].Value + ".png", true);
                }
                if (i % (items.Rows.Count / 10) == 0)
                    Console.WriteLine("Item Copy " + i / (items.Rows.Count / 10) + "0%");
            }
            sw.Close();
            fs.Close();

            fs = new FileStream("./output_drops.csv", FileMode.OpenOrCreate);
            sw = new StreamWriter(fs, Encoding.UTF8);
            sw.WriteLine("id,monsterid,itemid");
            int count = 1;
            for (int i = 0; i < md.Rows.Count; i++)
            {
                int monster_new_index = searchID(mobIDReflect, int.Parse((string)md.Rows[i][1]));
                int item_new_index = searchID(itemIDReflect, int.Parse((string)md.Rows[i][2]));
                if (monster_new_index != -1 && item_new_index != -1)
                {
                    sw.WriteLine(count + "," + mobIDReflect[monster_new_index].Value + "," + itemIDReflect[item_new_index].Value);
                    count++;
                }
            }
            sw.Close();
            fs.Close();

            fs = new FileStream("./output_item_reflect.csv", FileMode.OpenOrCreate);
            sw = new StreamWriter(fs, Encoding.UTF8);
            sw.WriteLine("old_item_id,new_item_id");
            for (int i = 0; i < itemIDReflect.Count; i++)
            {
                    sw.WriteLine(itemIDReflect[i].Key + "," + itemIDReflect[i].Value);
            }
            sw.Close();
            fs.Close();

            fs = new FileStream("./output_mob_reflect.csv", FileMode.OpenOrCreate);
            sw = new StreamWriter(fs, Encoding.UTF8);
            sw.WriteLine("old_mob_id,new_mob_id");
            for (int i = 0; i < mobIDReflect.Count; i++)
            {
                sw.WriteLine(mobIDReflect[i].Key + "," + mobIDReflect[i].Value);
            }
            sw.Close();
            fs.Close();

            

            //Console.ReadLine();
        }
    }
}
