using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Net.Mail;
using System.Reflection;
using System.Threading;

namespace SendMails
{
    class Program
    {
        public static string pwd = "";
        static void Main(string[] args)
        {
            Console.WriteLine("start");

          /*  if(args.Length == 0)
            {
                Console.WriteLine("Usage: sendemail.exe number for limit");
                return;
            }
            */
            DataSet emails = DBHelper.EmailSet();
            int rows = emails.Tables[0].Rows.Count;
            Console.WriteLine(string.Format("Rows:{0}", rows));



            string msg = @"Dear {0},<br><br>
                          You made an inquiry on {1} for one of our {2} properties. If you stayed at the property, then we are very interested in your opinion regarding your visit.
                          <br>
                          The property you made an inquiry was {3} in {4} {5}.<br>
                          This is a link to the property that you inquired about.<br>
                          <a href='{6}'>{6}</a><br><br>
                          This is the link the <b>Property's Review Page</b> where you can post your comments.<a href='{7}'>{7}</a><br> If you have any photos of the property and your stay you may upload them also.<br><br>
                          You will receive a 10% discount towards your next reservation after you complete the review.<br><br>
                          Linda Jenkins    <br><br>
                          CEO Vacations-Abroad.com         
                        ";

            int propid;
            string name="", email="", datetime="", url="",reviewurl="";
            DateTime standard_dt = new DateTime(2013,1,1);

            PropertyDetailInfo prop_info = new PropertyDetailInfo();

            int srows = 0;
            int sent_rows = 0, limit =0;
            Int32.TryParse(args[0], out limit);
            pwd = args[1];


            for (int i=0; i<rows; i++)
             {
                 DataRow row = emails.Tables[0].Rows[i];

                propid = Int32.Parse(row[0].ToString());
                name = row[1].ToString();
                string[] name_splits = name.Split(new char[] { ' ' }, StringSplitOptions.None);
                name = DBHelper.ToUpperFirstLetter(name_splits[0]);

                email = row[2].ToString();
                datetime = row[3].ToString();

                DateTime dt;
                 DateTime.TryParse(datetime, out dt);
                //  Console.WriteLine(dt.ToString("yyyy-MM-dd"));
                if (DateTime.Compare(dt, standard_dt) >= 0 )
                {

                    sent_rows++;
                    if (sent_rows < limit) continue;
                     prop_info = DBHelper.getPropertyDetailInfo(propid);


                    url = String.Format("https://www.vacations-abroad.com/{0}/{1}/{2}/{3}/default.aspx", prop_info.Country, prop_info.StateProvince, prop_info.City, prop_info.ID).ToLower().Replace(" ", "_");
                    reviewurl = String.Format("https://www.vacations-abroad.com/{0}/{1}/{2}/{3}/writereview.aspx", prop_info.Country, prop_info.StateProvince, prop_info.City, prop_info.ID).ToLower().Replace(" ", "_");
                    Console.WriteLine(String.Format("Origin No{4}:::No{3}  ==>Sending Email to {0} on {1} for Property {2}", email, datetime, propid, sent_rows,i));
                    string mssg;
                    mssg= String.Format(msg, name, datetime, prop_info.City, prop_info.Name2, prop_info.City, prop_info.Country, url, reviewurl);
                    Console.WriteLine(mssg);


                     DBHelper.SendEmail(email, String.Format("How Was your Trip to {0}", prop_info.City), mssg);
                  //  DBHelper.SendEmail("linda@vacations-abroad.com", String.Format("How Was your Trip to {0}", prop_info.City), mssg);

                    Thread.Sleep(9000);
                    
                }
                //Console.WriteLine(String.Format(msg, name,datetime, prop_info.City, prop_info.Name2, prop_info.City, prop_info.Country,url,reviewurl  ));

            }

            //DBHelper.SendEmail("linda@vacations-abroad.com", "How Was your Trip to", String.Format(msg, name, datetime, prop_info.City, prop_info.Name2, prop_info.City, prop_info.Country, url, reviewurl));
          //  DBHelper.SendEmail("andrew.li1987@yandex.com", "How Was your Trip to", String.Format(msg, name, datetime, prop_info.City, prop_info.Name2, prop_info.City, prop_info.Country, url, reviewurl));
            //Console.WriteLine(srows);
            Console.WriteLine("end");
        }
    }

    public class DBHelper
    {
        //public static string constring = "Data Source=WEB1; Integrated Security=True;Initial Catalog=herefordpies_test.stage;Persist Security Info=True;Packet Size=4096;Max Pool Size=200;Connection Timeout=10";
        public static string constring = "Data Source=69.89.14.163,1433; Integrated Security=false;Initial Catalog=herefordpies_test.stage;Connection Timeout=10;User ID=bookuser;Password=bookuser";

        public static string ToUpperFirstLetter(string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            // convert to char array of the string
            char[] letters = source.ToCharArray();
            // upper case the first char
            letters[0] = char.ToUpper(letters[0]);
            // return the array made of the new char array
            return new string(letters);
        }



        public static DataSet EmailSet()
        {
            DataSet dbset = new DataSet();

            using(SqlConnection sqlcon = new SqlConnection(constring))
            {
                sqlcon.Open();

                // string sql = String.Format("select count(*) from Users where (AccountType < 1 or AccountType is null) and Email=@email");
                string sql = String.Format("select PropertyID,ContactName,ContactEmail,ArrivalDate from Emails where ContactName is not null and ContactEmail is not null group by PropertyID,ContactName,contactEmail,ArrivalDate order by PropertyID");



                using (SqlCommand command = new SqlCommand(sql, sqlcon))
                {
                    using(SqlDataAdapter sqladapter = new SqlDataAdapter(command))
                    {
                        sqladapter.Fill(dbset, "Emails");
                    }
             
                }
                sqlcon.Close();
            }
            return dbset;
        }

        public static bool SendEmail(string toEmail, string subject, string msg)
        {
            //SmtpClient smtpclient = new SmtpClient("mail.vacations-abroad.com", 25);
   
            string emailbody = msg;

            MailMessage message = new MailMessage("noreply@vacations-abroad.com", toEmail);
            message.Subject = subject;
            message.Body = emailbody;
            message.IsBodyHtml = true;

            message.Body = message.Body.Replace("\r", "").Replace("\n", Environment.NewLine);

            SmtpClient smtpclient = new SmtpClient("smtp.vacations-abroad.com", 25);

            string crediental = Program.pwd;
            smtpclient.UseDefaultCredentials = false;

            smtpclient.Credentials = new System.Net.NetworkCredential("noreply@vacations-abroad.com", crediental);

            //smtpclient.UseDefaultCredentials = false;

            try
            {
                smtpclient.Send(message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
                throw ex;
               // return false;
            }
            return true;
        }
        public static PropertyDetailInfo getPropertyDetailInfo(int propid)
        {

            PropertyDetailInfo detail = new PropertyDetailInfo();
            //  adapter.Fill(customers, "Customers");
            try
            {
                using (SqlConnection con = new SqlConnection(constring))
                {
                    /*   @keyword nvarchar(200) ='',
                    @proptype int= 0,
                    @roomnum int= 0,
                    @amenityid int= 0
                    */
                    con.Open();
                    SqlCommand cmd = new SqlCommand("uspGetPropertiesDetailIno", con);
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@propid", SqlDbType.NVarChar, 200).Value = propid;

                    //   @pagenum int =0,
                    //@ratesort int= 0
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        PropertyInfo[] props = detail.GetType().GetProperties();
                        foreach (PropertyInfo prop_info in props)
                        {
                            try {
                                prop_info.SetValue(detail, Convert.ChangeType(reader[prop_info.Name], prop_info.PropertyType), null);
                            }
                            catch(Exception e)
                            {
                            }
                            

                        }

                    }

                    reader.Close();
                    con.Close();

                }
            }
            catch (Exception ex)
            {

            }
            return detail;
        }
    }

    public class PropertyDetailInfo
    {
        //.ID,.Name,.Address,.NumBedrooms,.NumBaths, .NumSleeps, .NumTVs,.NumVCRs, .NumCDPlayers,.Name2, .MinNightRate,.HiNightRate,.City,.StateProvince,.Country,.PropertyName
        public int ID { get; set; }
        public int UserID { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int NumBedrooms { get; set; }
        public int NumBaths { get; set; }
        public int NumSleeps { get; set; }
        public int NumTVs { get; set; }
        public int NumVCRs { get; set; }
        public int NumCDPlayers { get; set; }
        public string Name2 { get; set; }
        public int MinNightRate { get; set; }
        public int HiNightRate { get; set; }
        public int MinimumNightlyRentalID { get; set; }
        public string MinRateCurrency { get; set; }
        public string City { get; set; }
        public string StateProvince { get; set; }
        public string Country { get; set; }
        public string PropertyName { get; set; }
        public string CategoryTypes { get; set; }
        public int Category { get; set; }
        public string FileName { get; set; }
    }

}
