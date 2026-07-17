using PMS.DTO.ViewModels.MovieNightsViewModels;
using PMS.EF;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.StudentPortal.MovieNights
{
    public interface IMovieNightsAdmin
    {
        //Movies
        List<Movie> GetMovies();
        Movie GetMovieById(int movieID);
        Movie AddMovie(MoviesVM movieVM);
        bool UpdateMovie(MoviesVM movieVM);
        bool DeleteMovie(int movieID);


        //Movie Shows
        List<MovieShow> GetMovieShows();
        List<MovieShowsVM> GetMovieShowsVM();
        MovieShow GetMovieShowById(int movieShowID);
        MovieShow AddMovieShow(MovieShowsVM movieShowsVM);
        bool UpdateMovieShow(MovieShowsVM movieShowsVM);
        bool DeleteMovieShow(int movieShowID);


        //Movie Vote Campaigns
        List<MovieVoteCampaign> GetMovieVoteCampaigns();
        MovieVoteCampaign GetMovieVoteCampaignByID(int id);
        MovieVoteCampaign AddMovieVoteCampaign(MovieVoteCampaignVM movieVoteCampaignVM);
        bool UpdateMovieVoteCampaign(MovieVoteCampaignVM movieVoteCampaignVM);
        bool DeleteMovieVoteCampaign(int movieVoteCampaignId);


        //Movie Vote Campaign Details
        List<MovieVoteCampaignDetailVM> GetMovieVoteCampaignDetail(int movieVoteCampaignId);
        List<MoviesVM> GetMov();
        MovieVoteCampaignDetailVM GetMovieVoteCampaignDetailbyID(int movieVoteCampaignDetailId);
        MovieVoteCampaignDetail AddMovieVoteCampaignDetail(MovieVoteCampaignDetailVM movieVoteCampaignDetailVM);
        bool UpdateMovieVoteCampaignDetail(MovieVoteCampaignDetailVM movieVoteCampaignDetailVM);
        bool DeleteMovieVoteCampaignDetail(int movieVoteCampaignDetailId);
    }
}
