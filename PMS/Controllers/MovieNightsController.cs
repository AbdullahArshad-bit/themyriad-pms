using PMS.Common.Filters;
using PMS.DTO.ViewModels.MovieNightsViewModels;
using PMS.Services.Services.StudentPortal.MovieNights;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using PMS.Common.Classes;

namespace PMS.Controllers
{
    [AuthorizeUser]
    public class MovieNightsController : BaseController
    {
        private readonly IMovieNightsAdmin movieNightsAdmin;
        public MovieNightsController(IMovieNightsAdmin _movieNightsAdmin)
        {
            movieNightsAdmin = _movieNightsAdmin;
        }

        [AuthorizeUser(Roles = AppUserRoles.view_movies)]
        [Route("movies")]
        public ActionResult Movies()
        {
            ViewBag.Movies = movieNightsAdmin.GetMovies();
            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.add_movie)]
        [Route("add-movies/{movieID?}")]
        public ActionResult AddMovie(int? movieID)
        {
            MoviesVM model = new MoviesVM();

            if (movieID > 0)
            {
                var obj = movieNightsAdmin.GetMovieById(Convert.ToInt32(movieID));
                if (obj != null)
                {
                    model = new MoviesVM
                    {
                        MovieID = obj.MovieID,
                        MovieTitle = obj.MovieTitle,
                        MovieDetails = obj.MovieDetails,
                        BannerImageUrl = obj.BannerImageUrl,
                        MonthImageUrl = obj.MonthImageUrl,
                        VoteImageUrl = obj.VoteImageUrl
                    };
                }
            }

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_movie)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddNewMovie(MoviesVM moviesVM)
        {
            try
            {
                if (moviesVM.MovieID > 0)
                {
                    ModelState.Remove("BannerImage");
                    ModelState.Remove("MonthImage");
                    ModelState.Remove("VoteImage");
                }

                if (ModelState.IsValid)
                {
                    if (moviesVM.MovieID > 0)
                    {
                        if (movieNightsAdmin.UpdateMovie(moviesVM))
                        {
                            TempData["success"] = "Movie updated succesfully";
                            return RedirectToAction("Movies");
                        }

                        ViewBag.error = "Something went wrong, movie not updated.";
                    }
                    else
                    {
                        if (movieNightsAdmin.AddMovie(moviesVM).MovieID > 0)
                        {
                            TempData["success"] = "Movie added succesfully";
                            return RedirectToAction("Movies");
                        }

                        ViewBag.error = "Something went wrong, movie not saved.";
                    }
                }
                else
                {
                    ViewBag.error = Common.Helper.GetModelError(ModelState);
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }

            return View("AddMovie", moviesVM);
        }

        [AuthorizeUser(Roles = AppUserRoles.delete_movies)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMovie(int movieID)
        {
            try
            {
                if (movieNightsAdmin.DeleteMovie(movieID))
                {
                    TempData["success"] = "Movie deleted successfully.";
                }
                else
                {
                    TempData["error"] = "Sorry something went wrong, movie not deleted.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("Movies");
        }

        [AuthorizeUser(Roles = AppUserRoles.view_movie_shows)]
        [Route("movie-shows")]
        public ActionResult MovieShows()
        {
            ViewBag.MovieShows = movieNightsAdmin.GetMovieShowsVM();
            return View();
        }

        [AuthorizeUser(Roles = AppUserRoles.add_movie_shows)]
        [Route("add-movie-shows/{showID?}")]
        public ActionResult AddMovieShows(int? showID)
        {
            MovieShowsVM model = new MovieShowsVM();
            model.MoviesList = movieNightsAdmin.GetMovies();
            model.ShowDate = DateTime.Today;

            if (showID > 0)
            {
                var obj = movieNightsAdmin.GetMovieShowById(Convert.ToInt32(showID));
                if (obj != null)
                {
                    model.MovieID = obj.MovieID;
                    model.MovieShowID = obj.MovieShowID;
                    model.HallNo = obj.HallNo;
                    model.ShowDate = Convert.ToDateTime(obj.ShowDate);
                    model.ShowTime = obj.ShowTime;
                    model.ShowDetails = obj.ShowDetails;
                }
            }

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_movie_shows)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddNewMovieShows(MovieShowsVM movieShowsVM)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (movieShowsVM.MovieShowID > 0)
                    {
                        if (movieNightsAdmin.UpdateMovieShow(movieShowsVM))
                        {
                            TempData["success"] = "Movie show updated succesfully";
                            return RedirectToAction("MovieShows");
                        }

                        ViewBag.error = "Something went wrong, movie show not updated.";
                    }
                    else
                    {
                        if (movieNightsAdmin.AddMovieShow(movieShowsVM).MovieShowID > 0)
                        {
                            TempData["success"] = "Movie show added succesfully";
                            return RedirectToAction("MovieShows");
                        }

                        ViewBag.error = "Something went wrong, movie show not saved.";
                    }
                }
                else
                {
                    ViewBag.error = Common.Helper.GetModelError(ModelState);
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message;
            }

            movieShowsVM.MoviesList = movieNightsAdmin.GetMovies();
            return View("AddMovieShows", movieShowsVM);
        }


        [AuthorizeUser(Roles = AppUserRoles.delete_movie_shows)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMovieShows(int showID)
        {
            try
            {
                if (movieNightsAdmin.DeleteMovieShow(showID))
                {
                    TempData["success"] = "Movie Show deleted successfully.";
                }
                else
                {
                    TempData["error"] = "Sorry something went wrong, movie show not deleted.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("MovieShows");
        }


        [AuthorizeUser(Roles = AppUserRoles.view_movie_vote_campaign)]
        [Route("movie-vote-campaign")]
        public ActionResult MovieVoteCampaign()
        {
            var model = movieNightsAdmin.GetMovieVoteCampaigns();
            return View(model);
        }


        [AuthorizeUser(Roles = AppUserRoles.add_movie_vote_campaign)]
        [Route("add-movie-vote-campaign/{movieVoteCampaignID?}")]
        public ActionResult AddMovieVoteCampaign(int? movieVoteCampaignID)
        {
            MovieVoteCampaignVM model = new MovieVoteCampaignVM
            {
                ShowDate = DateTime.Today,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(7),
                IsActive = true,
                Description = ""
            };
            try
            {
                if (movieVoteCampaignID > 0)
                {
                    var campaign = movieNightsAdmin.GetMovieVoteCampaignByID(Convert.ToInt32(movieVoteCampaignID));
                    if (campaign != null)
                    {
                        model = new MovieVoteCampaignVM
                        {
                            MovieVoteCampaignID = campaign.MovieVoteCampaignID,
                            ShowDate = campaign.ShowDate,
                            ShowTime = campaign.ShowTime,
                            HallNo = campaign.HallNo,
                            StartDate = campaign.StartDate,
                            EndDate = campaign.EndDate,
                            IsActive = campaign.IsActive,
                            Description = campaign.Description
                        };
                    }
                    else
                    {
                        TempData["error"] = "Sorry specified record not found.";
                        return RedirectToAction("MovieVoteCampaign");
                    }
                }
                else
                {

                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message.ToString();
            }

            return View(model);
        }


        [AuthorizeUser(Roles = AppUserRoles.add_movie_vote_campaign)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddNewMovieVoteCampaign(MovieVoteCampaignVM movieVoteCampaignVM)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (movieVoteCampaignVM.MovieVoteCampaignID > 0)
                    {
                        if (movieNightsAdmin.UpdateMovieVoteCampaign(movieVoteCampaignVM))
                        {
                            TempData["success"] = "Movie Vote Campaign updated succesfully";
                            return RedirectToAction("MovieVoteCampaign");
                        }

                        ViewBag.error = "Something went wrong, Movie Vote Campaign not updated.";
                    }
                    else
                    {
                        if (movieNightsAdmin.AddMovieVoteCampaign(movieVoteCampaignVM).MovieVoteCampaignID > 0)
                        {
                            TempData["success"] = "Movie Vote Campaign added succesfully";
                            return RedirectToAction("MovieVoteCampaign");
                        }

                        ViewBag.error = "Something went wrong, Movie Vote Campaign not saved.";
                    }
                }
                else
                {
                    ViewBag.error = Common.Helper.GetModelError(ModelState);
                }
            }
            catch (Exception ex)
            {
                ViewBag.error = ex.Message.ToString();
            }

            return View("AddMovieVoteCampaign", movieVoteCampaignVM);
        }


        [AuthorizeUser(Roles = AppUserRoles.delete_movie_vote_campaign)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMovieVoteCampaign(int movieVoteCampaignID)
        {
            try
            {
                if (movieNightsAdmin.DeleteMovieVoteCampaign(movieVoteCampaignID))
                {
                    TempData["success"] = "Movie Vote Campaign deleted successfully.";
                }
                else
                {
                    TempData["error"] = "Sorry something went wrong, Movie Vote Campaign not deleted.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("MovieVoteCampaign");
        }


        [AuthorizeUser(Roles = AppUserRoles.view_movie_vote_campaign_detail)]
        [Route("movie-vote-campaign-detail/{movieVoteCampaignID}/{movieVoteCampaignDetailID?}")]
        public ActionResult MovieVoteCampaignDetail(int movieVoteCampaignID, int? movieVoteCampaignDetailID)
        {
            MovieVoteCampaignDetailVM model = new MovieVoteCampaignDetailVM
            {
                MoviesList = movieNightsAdmin.GetMovies(),
                MovieVoteCampaignID = movieVoteCampaignID
            };

            model.MovieVoteCampaignDetailID = (movieVoteCampaignDetailID == null) ? 0 : Convert.ToInt32(movieVoteCampaignDetailID);

            var details = movieNightsAdmin.GetMovieVoteCampaignDetail(movieVoteCampaignID);
            foreach (var d in details)
            {
                model.MoviesList.Remove(model.MoviesList.Find(x => x.MovieID == d.MovieID && d.MovieVoteCampaignDetailID != movieVoteCampaignDetailID));
            }

            if (movieVoteCampaignDetailID != null)
            {
                var camp = movieNightsAdmin.GetMovieVoteCampaignDetailbyID(Convert.ToInt32(movieVoteCampaignDetailID));
                model.MovieID = camp.MovieID;
            }
            else
            {
                ViewBag.CampaignDetail = details;
            }

            return View(model);
        }

        [AuthorizeUser(Roles = AppUserRoles.add_movie_vote_campaign_detail)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult AddMovieVoteCampaignDetail(MovieVoteCampaignDetailVM movieVoteCampaignDetailVM)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (movieVoteCampaignDetailVM.MovieVoteCampaignDetailID == 0)
                    {
                        if (movieNightsAdmin.AddMovieVoteCampaignDetail(movieVoteCampaignDetailVM).MovieVoteCampaignDetailID > 0)
                        {
                            TempData["success"] = "Movie Campaign Detail saved successfully.";
                        }
                        else
                        {
                            TempData["error"] = "Sorry something went wrong, Movie Campaign Detail not updated.";
                        }
                    }
                    else
                    {
                        if (movieNightsAdmin.UpdateMovieVoteCampaignDetail(movieVoteCampaignDetailVM))
                        {
                            TempData["success"] = "Movie Campaign Detail updated successfully.";
                        }
                        else
                        {
                            TempData["error"] = "Sorry something went wrong, Movie Campaign Detail not updated.";
                        }
                    }
                }
                else
                {
                    TempData["error"] = Common.Helper.GetModelError(ModelState);
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("MovieVoteCampaignDetail", new { movieVoteCampaignID = movieVoteCampaignDetailVM.MovieVoteCampaignID });
        }

        [AuthorizeUser(Roles = AppUserRoles.delete_movie_vote_campaign_detail)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteMovieVoteCampaignDetail(int movieVoteCampaignID, int movieVoteCampaignDetailID)
        {
            try
            {
                if (movieNightsAdmin.DeleteMovieVoteCampaignDetail(movieVoteCampaignDetailID))
                {
                    TempData["success"] = "Movie Campaign Detail deleted successfully.";
                }
                else
                {
                    TempData["error"] = "Sorry something went wrong, Movie Campaign Detail not deleted.";
                }
            }
            catch (Exception ex)
            {
                TempData["error"] = ex.Message;
            }

            return RedirectToAction("MovieVoteCampaignDetail", new { movieVoteCampaignID = movieVoteCampaignID });
        }
    }
}