namespace postbot
{
   internal class User
   {
      public int id { get; set; }
      public string id_user { get; set; }
      public string chanels { get; set; }
      public User(int id, string id_user, string chanels)
      {
         this.id = id;
         this.id_user = id_user;
         this.chanels = chanels;
      }
   }
}