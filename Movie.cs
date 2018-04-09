using System;
using System.Text;

namespace databases {
    class Movie {
        private String Title;
        private int Year;
        private String Director;
        private decimal IMDB_rating;
        private String Genre;

        public Movie (String title, int year, String director, decimal imdb_rating, String genre) {
            Title1 = title;
            Year1 = year;
            Director1 = director;
            IMDB_rating1 = imdb_rating;
            Genre1 = genre;
        }

        public string Title1 { get => Title; set => Title = value; }
        public int Year1 { get => Year; set => Year = value; }
        public string Director1 { get => Director; set => Director = value; }
        public decimal IMDB_rating1 { get => IMDB_rating; set => IMDB_rating = value; }
        public string Genre1 { get => Genre; set => Genre = value; }

        public override String ToString () {
            StringBuilder sb = new StringBuilder ();
            sb.Append (Title1 + ", ");
            sb.Append (Year1 + ", ");
            sb.Append (Director1 + ", ");
            sb.Append (IMDB_rating1 + ", ");
            sb.Append (Genre1);
            return sb.ToString ();
        }
    }
}