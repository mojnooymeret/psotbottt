namespace postbot
{
   internal class Post
   {
      public int id { get; set; }
      public string id_user { get; set; }
      public string id_chanel { get; set; }
      public string text { get; set; }
      public string media { get; set; }
      public string date { get; set; }
      public string status { get; set; }
      public Post(int id, string id_user, string id_chanel, string text, string media, string date, string status)
      {
         this.id = id;
         this.id_user = id_user;
         this.id_chanel = id_chanel;
         this.text = text;
         this.media = media;
         this.date = date;
         this.status = status;
      }
   }
}