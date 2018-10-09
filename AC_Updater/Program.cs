using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using System.IO;

namespace AC_Updater
{
    class Program
    {
        private static MySqlConnection connection;
        private static string server="localhost";
        private static string database="proxeeus_db";
        //private static string database = "neq";
        private static string uid="root";
        private static string password="eqemu";



        static void Main(string[] args)
        {


            Console.WriteLine("Opening connection...");

            Console.WriteLine("Connection opened.");

            //ProcessAC();
            //ProcessHP();
            AdjustHP();
        }

        private static void AdjustHP()
        {
            var zone = "paw";
            var listProx = new List<NPC>();
            // Load Proxeeus
            var connectionString = "SERVER=" + server + ";" + "DATABASE=proxeeus_db;" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            connection = new MySqlConnection(connectionString);
            OpenConnection();
            var query = @"SELECT nt.id,nt.hp, nt.level, nt.name, nt.special_abilities, s2.x as Spawn2X, s2.y as Spawn2Y, s2.z as Spawn2Z, sg.name as spawngroup_name,sg.id as Spawngroup_id, sg.min_x as Spawngroup_minX, sg.max_x as Spawngroup_maxX, sg.min_y as Spawngroup_minY, sg.max_y as Spawngroup_maxY, sg.dist as Spawngroup_dist, sg.mindelay as Spawngroup_mindelay, sg.delay as Spawngroup_delay
                            FROM spawn2 s2 
                            JOIN spawngroup sg ON sg.id = s2.spawngroupid 

                            JOIN spawnentry se
                            ON se.spawngroupid = sg.id 
                            JOIN npc_types nt 
                            ON nt.id = se.npcid 
                            WHERE s2.zone = '" + zone + "';";
            var cmd = new MySqlCommand(query, connection);
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var npc = new NPC() { HP = reader["hp"].ToString(), Id = reader["id"].ToString(), Level = reader["level"].ToString(), Name = reader["name"].ToString() };
                listProx.Add(npc);
            }

            reader.Close();
            Program.CloseConnection();

            var queries = new List<string>();
            var revertQueries = new List<string>();
            var cleanProx = listProx.GroupBy(x => x.Id).Select(x => x.First()).ToList();
            var percent = 28.5;
            queries.Add(string.Format("-- {0}% HP adjustment script for {1}.", percent.ToString(), zone));
            revertQueries.Add(string.Format("-- REVERT {0}% HP adjustment script for {1}.", percent.ToString(), zone));
            foreach (var proxNpc in cleanProx)
            {
                if(proxNpc.Name != "Player_Bot")
                {
                    var decreasePercent = Math.Ceiling(Convert.ToInt32(proxNpc.HP) - (Convert.ToInt32(proxNpc.HP) * percent / 100));
                    // update queries
                    queries.Add(string.Format("update npc_types set hp='{0}' where name='{1}' and level='{2}' and id='{3}'", decreasePercent, proxNpc.Name, proxNpc.Level, proxNpc.Id));
                    // revert queries
                    revertQueries.Add(string.Format("update npc_types set hp='{0}' where name='{1}' and level='{2}' and id='{3}'", proxNpc.HP, proxNpc.Name, proxNpc.Level, proxNpc.Id));
                }

            }

            using (var streamWriter = new StreamWriter(string.Format(@"c:\adjust_{0}.sql", zone)))
            {
                foreach (var adjQuery in queries)
                    streamWriter.WriteLine(adjQuery);
            }
            using (var streamWriter = new StreamWriter(string.Format(@"c:\adjust_{0}_REVERT.sql", zone)))
            {
                foreach (var adjQuery in revertQueries)
                    streamWriter.WriteLine(adjQuery);
            }
            Console.ReadLine();
        }

        private static void ProcessHP()
        {
            // Load NEQ
            var connectionString = "SERVER=" + server + ";" + "DATABASE=neq;" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            connection = new MySqlConnection(connectionString);
            OpenConnection();
            var listNEQ = new List<NPC>();
            var listProx = new List<NPC>();
            var query = @"SELECT nt.id,nt.hp, nt.level, nt.name, nt.special_abilities, s2.x as Spawn2X, s2.y as Spawn2Y, s2.z as Spawn2Z, sg.name as spawngroup_name,sg.id as Spawngroup_id, sg.min_x as Spawngroup_minX, sg.max_x as Spawngroup_maxX, sg.min_y as Spawngroup_minY, sg.max_y as Spawngroup_maxY, sg.dist as Spawngroup_dist, sg.mindelay as Spawngroup_mindelay, sg.delay as Spawngroup_delay
                            FROM spawn2 s2 
                            JOIN spawngroup sg ON sg.id = s2.spawngroupid 

                            JOIN spawnentry se
                            ON se.spawngroupid = sg.id 
                            JOIN npc_types nt 
                            ON nt.id = se.npcid 
                            WHERE s2.zone = 'ecommons' ";

            var cmd = new MySqlCommand(query, connection);
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var npc = new NPC() { HP = reader["hp"].ToString(), Id = reader["id"].ToString(), Level = reader["level"].ToString(), Name = reader["name"].ToString() };
                listNEQ.Add(npc);
            }

            reader.Close();
            Program.CloseConnection();

            // Load Proxeeus
            connectionString = "SERVER=" + server + ";" + "DATABASE=proxeeus_db;" + "UID=" + uid + ";" + "PASSWORD=" + password + ";";
            connection = new MySqlConnection(connectionString);
            OpenConnection();

            cmd = new MySqlCommand(query, connection);
            reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var npc = new NPC() { HP = reader["hp"].ToString(), Id = reader["id"].ToString(), Level = reader["level"].ToString(), Name = reader["name"].ToString() };
                listProx.Add(npc);
            }

            reader.Close();
            Program.CloseConnection();

            var queries = new List<string>();
            var revertQueries = new List<string>();
            //var cleanNeq = listNEQ.GroupBy(x => x.Id).Select(x => x.First()).ToList();
            //var cleanProx = listProx.GroupBy(x => x.Id).Select(x => x.First()).ToList();

            foreach(var proxNpc in listProx)
                foreach(var neqNpc in listNEQ)
                {
                    if (proxNpc.Name == neqNpc.Name && proxNpc.Level == neqNpc.Level)
                    {
                        // update queries
                        queries.Add(string.Format("update npc_types set hp='{0}' where name='{1}' and level='{2}' and id='{3}'", neqNpc.HP, proxNpc.Name, proxNpc.Level, proxNpc.Id));
                        // revert queries
                        revertQueries.Add(string.Format("update npc_types set hp='{0}' where name='{1}' and level='{2}' and id='{3}'", proxNpc.HP, proxNpc.Name, proxNpc.Level, proxNpc.Id));
                    }
                        
                }
            Console.ReadLine();
        }

        private static void ProcessAC()
        {
            var query = "select * from npc_types";
            var cmd = new MySqlCommand(query, connection);
            var reader = cmd.ExecuteReader();

            using (var writer = new StreamWriter(@"C:\Users\LENOVO\Desktop\mob_update.sql"))
            {
                writer.Write("-- AC Updater script, generated by an automated tool. Data taken from EQEmu's research.");
                while (reader.Read())
                {
                    var newAC = 0;
                    if(reader["level"].ToString() == "1" || reader["level"].ToString() == "2")
                    {
                         newAC = Convert.ToInt32(reader["level"].ToString()) * 3 + 2;
                    }
                    else if (Convert.ToInt32(reader["level"]) >= 3  && Convert.ToInt32(reader["level"]) < 15)
                    {
                         newAC = Convert.ToInt32(reader["level"]) * 3;
                    }
                    else if (Convert.ToInt32(reader["level"]) >= 15 && Convert.ToInt32(reader["level"]) < 50)
                    {
                         newAC = (int)Math.Floor(Convert.ToInt32(reader["level"]) * 4.1 - 15);
                    }
                    else if (Convert.ToInt32(reader["level"]) >= 50)
                    {
                         newAC = 200;
                    }
                    Console.WriteLine(string.Format("New AC for {0} is {1} (updated from {2}).", reader["name"], newAC, reader["level"]));
                    writer.Write("update npc_types set ac = {0} where id={1};\n", newAC, reader["id"]);
                }
                writer.Write("-- End script");
            }

            reader.Close();
            Program.CloseConnection();
            Console.ReadLine();
        }

        //open connection to database
        private static bool OpenConnection()
        { 
            try
            {
                connection.Open();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
                
            }
            return false;
        }

        //Close connection
        static bool CloseConnection()
        {
            try
            {
                connection.Close();
                return true;
            }
            catch (MySqlException ex)
            {
                Console.WriteLine(ex.Message);
                return false;
            }
        }

    }
}
