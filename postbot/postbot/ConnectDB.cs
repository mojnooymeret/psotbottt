using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace postbot
{
   internal class ConnectDB
   {
      public static SQLiteDataReader Query(string str)
      {
         SQLiteConnection SQLiteConnection = new SQLiteConnection("Data Source=|DataDirectory|post.db");
         SQLiteCommand SQLiteCommand = new SQLiteCommand(str, SQLiteConnection);
         try {
            SQLiteConnection.Open();
            SQLiteDataReader reader = SQLiteCommand.ExecuteReader();
            return reader;
         } catch { return null; }
      }

      public static void LoadUser(List<User> data)
      {
         data.Clear();
         SQLiteDataReader query = Query("select * from `User`;");
         if (query != null) {
            while (query.Read()) {
               data.Add(new User(
                  Convert.ToInt32(query.GetValue(0)),
                  query.GetValue(1).ToString(),
                  query.GetValue(2).ToString()
               ));
            }
         }
      }

      public static void LoadPost(List<Post> data)
      {
         data.Clear();
         SQLiteDataReader query = Query("select * from `Post`;");
         if (query != null) {
            while (query.Read()) {
               data.Add(new Post(
                  Convert.ToInt32(query.GetValue(0)),
                  query.GetValue(1).ToString(),
                  query.GetValue(2).ToString(),
                  query.GetValue(3).ToString(),
                  query.GetValue(4).ToString(),
                  query.GetValue(5).ToString(),
                  query.GetValue(6).ToString()
               ));
            }
         }
      }

      public static void LoadLoop(List<Loop> data)
      {
         data.Clear();
         SQLiteDataReader query = Query("select * from `Loop`;");
         if (query != null) {
            while (query.Read()) {
               data.Add(new Loop(
                  Convert.ToInt32(query.GetValue(0)),
                  query.GetValue(1).ToString(),
                  query.GetValue(2).ToString(),
                  query.GetValue(3).ToString(),
                  query.GetValue(4).ToString(),
                  query.GetValue(5).ToString(),
                  query.GetValue(6).ToString(),
                  query.GetValue(7).ToString(),
                  query.GetValue(8).ToString()
               ));
            }
         }
      }
   }
}