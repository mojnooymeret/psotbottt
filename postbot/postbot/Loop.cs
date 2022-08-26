namespace postbot
{
   internal class Loop
   {
      public int id { get; set; }
      public string id_user { get; set; }
      public string id_chanels { get; set; }
      public string name { get; set; }
      public string posts { get; set; }
      public string time_interval { get; set; }
      public string last_send { get; set; }
      public string last_post { get; set; }
      public string process { get; set; }
      public Loop(int id, string id_user, string id_chanels, string name, string posts, string time_interval, string last_send, string last_post, string process)
      {
         this.id = id;
         this.id_user = id_user;
         this.id_chanels = id_chanels;
         this.name = name;
         this.posts = posts;
         this.time_interval = time_interval;
         this.last_send = last_send;
         this.last_post = last_post;
         this.process = process;
      }
   }
}