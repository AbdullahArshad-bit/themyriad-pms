using PMS.Common;
using PMS.DTO.ViewModels.MovieNightsViewModels;
using PMS.EF;
using PMS.Repository.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PMS.Services.Services.StudentPortal.MovieNights
{
    public class MovieNightsAdmin : IMovieNightsAdmin
    {
        private readonly UnitOfWork<PMSEntities> uow;
        public MovieNightsAdmin(UnitOfWork<PMSEntities> _uow)
        {
            uow = _uow;
        }
        public Movie AddMovie(MoviesVM movieVM)
        {
            try
            {
                ImageResult result = new ImageResult();

                Common.ImageUpload upload = new Common.ImageUpload()
                {
                    Quality = 80
                };
                result = upload.RenameUploadFile(movieVM.BannerImage);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                string bannerImageUrl = result.ImageName;

                upload = new Common.ImageUpload()
                {
                    Quality = 80
                };
                result = upload.RenameUploadFile(movieVM.MonthImage);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                string monthImageUrl = result.ImageName;


                upload = new Common.ImageUpload()
                {
                    Quality = 80
                };
                result = upload.RenameUploadFile(movieVM.VoteImage);

                if (!result.Success)
                    throw new Exception(result.ErrorMessage);

                string voteImageUrl = result.ImageName;



                EF.Movie movie = new Movie
                {
                    MovieTitle = movieVM.MovieTitle,
                    MovieDetails = movieVM.MovieDetails,
                    BannerImageUrl = bannerImageUrl,
                    MonthImageUrl = monthImageUrl,
                    VoteImageUrl = voteImageUrl,
                    IsEnable = true,
                    CreatedDate = DateTime.Now
                };

                uow.GenericRepository<Movie>().Insert(movie);
                uow.SaveChanges();

                return movie;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public MovieShow AddMovieShow(MovieShowsVM movieShowsVM)
        {
            try
            {
                MovieShow movieShow = new MovieShow
                {
                    MovieID = movieShowsVM.MovieID,
                    ShowDate = movieShowsVM.ShowDate,
                    ShowTime = movieShowsVM.ShowTime,
                    HallNo = movieShowsVM.HallNo,
                    ShowDetails = movieShowsVM.ShowDetails,
                    IsEnable = true,
                    CreatedDate = DateTime.Now
                };

                uow.GenericRepository<MovieShow>().Insert(movieShow);
                uow.SaveChanges();
                return movieShow;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool DeleteMovie(int movieID)
        {
            try
            {
                Movie movie = GetMovieById(movieID);
                if (movie != null)
                {
                    movie.IsEnable = false;
                    uow.GenericRepository<Movie>().Update(movie);
                    uow.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool DeleteMovieShow(int movieShowID)
        {
            try
            {
                MovieShow movieShow = GetMovieShowById(movieShowID);
                if (movieShow != null)
                {
                    movieShow.IsEnable = false;
                    uow.GenericRepository<MovieShow>().Update(movieShow);
                    uow.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public Movie GetMovieById(int movieID)
        {
            try
            {
                return uow.GenericRepository<Movie>().Table.FirstOrDefault(x => x.IsEnable == true && x.MovieID == movieID);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<Movie> GetMovies()
        {
            try
            {
                return uow.GenericRepository<Movie>().GetAll().Where(x => x.IsEnable == true).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public MovieShow GetMovieShowById(int movieShowID)
        {
            try
            {
                return uow.GenericRepository<MovieShow>().Table.FirstOrDefault(x => x.IsEnable == true && x.MovieShowID == movieShowID);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<MovieShow> GetMovieShows()
        {
            try
            {
                return uow.GenericRepository<MovieShow>().GetAll().Where(x => x.IsEnable == true).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public List<MovieShowsVM> GetMovieShowsVM()
        {
            try
            {
                return uow.Context.MovieShows.Include("Movie").Where(x => x.IsEnable == true).Select
                    (x => new MovieShowsVM
                    {
                        MovieID = x.MovieID,
                        MovieShowID = x.MovieShowID,
                        MovieTitle = x.Movie.MovieTitle,
                        HallNo = x.HallNo,
                        ShowDate = x.ShowDate,
                        ShowTime = x.ShowTime,
                        ShowDetails = x.ShowDetails
                    }).ToList();

                //return uow.GenericRepository<MovieShow>().Table..GetAll().Where(x => x.IsEnable == true).Select
                //    (x => new MovieShowsVM
                //    {
                //        MovieID = x.MovieID,
                //        MovieShowID = x.MovieShowID,
                //        MovieTitle = x.Movie.MovieTitle,
                //        HallNo = x.HallNo,
                //        ShowDate = x.ShowDate,
                //        ShowTime = x.ShowTime,
                //        ShowDetails = x.ShowDetails
                //    }).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool UpdateMovie(MoviesVM movieVM)
        {
            try
            {
                Movie movie = GetMovieById(movieVM.MovieID);
                if (movie != null)
                {
                    ImageResult result = new ImageResult();

                    Common.ImageUpload upload = new Common.ImageUpload()
                    {
                        Quality = 80
                    };
                    if (movieVM.BannerImage != null)
                    {
                        result = upload.RenameUploadFile(movieVM.BannerImage);

                        if (!result.Success)
                            throw new Exception(result.ErrorMessage);

                        movie.BannerImageUrl = result.ImageName;
                    }

                    if (movieVM.MonthImage != null)
                    {
                        upload = new Common.ImageUpload()
                        {
                            Quality = 80
                        };
                        result = upload.RenameUploadFile(movieVM.MonthImage);

                        if (!result.Success)
                            throw new Exception(result.ErrorMessage);

                        movie.MonthImageUrl = result.ImageName;
                    }


                    if (movieVM.VoteImage != null)
                    {
                        upload = new Common.ImageUpload()
                        {
                            Quality = 80
                        };
                        result = upload.RenameUploadFile(movieVM.VoteImage);

                        if (!result.Success)
                            throw new Exception(result.ErrorMessage);

                        movie.VoteImageUrl = result.ImageName;
                    }


                    movie.MovieTitle = movieVM.MovieTitle;
                    movie.MovieDetails = movieVM.MovieDetails;

                    uow.GenericRepository<Movie>().Update(movie);
                    uow.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool UpdateMovieShow(MovieShowsVM movieShowsVM)
        {
            try
            {
                MovieShow movieShow = GetMovieShowById(movieShowsVM.MovieShowID);
                if (movieShow != null)
                {
                    movieShow.MovieID = movieShowsVM.MovieID;
                    movieShow.ShowDate = movieShowsVM.ShowDate;
                    movieShow.ShowTime = movieShowsVM.ShowTime;
                    movieShow.HallNo = movieShowsVM.HallNo;
                    movieShow.ShowDetails = movieShowsVM.ShowDetails;

                    uow.GenericRepository<MovieShow>().Update(movieShow);
                    uow.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        #region Movie Vote Campaigns

        public List<MovieVoteCampaign> GetMovieVoteCampaigns()
        {
            return uow.GenericRepository<MovieVoteCampaign>().Table.Where(x => x.IsEnable == true).ToList();
        }
        public MovieVoteCampaign GetMovieVoteCampaignByID(int id)
        {
            return uow.GenericRepository<MovieVoteCampaign>().GetById(id);
        }
        public MovieVoteCampaign AddMovieVoteCampaign(MovieVoteCampaignVM movieVoteCampaignVM)
        {
            try
            {
                MovieVoteCampaign movieVoteCampaign = new MovieVoteCampaign
                {
                    Description = movieVoteCampaignVM.Description,
                    ShowDate = movieVoteCampaignVM.ShowDate,
                    ShowTime = movieVoteCampaignVM.ShowTime,
                    HallNo = movieVoteCampaignVM.HallNo,
                    StartDate = movieVoteCampaignVM.StartDate,
                    EndDate = movieVoteCampaignVM.EndDate,
                    IsActive = movieVoteCampaignVM.IsActive,
                    IsEnable = true,
                    CreatedDate = DateTime.Now
                };

                uow.GenericRepository<MovieVoteCampaign>().Insert(movieVoteCampaign);
                uow.SaveChanges();
                return movieVoteCampaign;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public bool UpdateMovieVoteCampaign(MovieVoteCampaignVM movieVoteCampaignVM)
        {
            try
            {
                MovieVoteCampaign movieVoteCampaign = GetMovieVoteCampaignByID(movieVoteCampaignVM.MovieVoteCampaignID);
                if (movieVoteCampaign != null)
                {
                    movieVoteCampaign.Description = movieVoteCampaignVM.Description;
                    movieVoteCampaign.ShowDate = movieVoteCampaignVM.ShowDate;
                    movieVoteCampaign.ShowTime = movieVoteCampaignVM.ShowTime;
                    movieVoteCampaign.HallNo = movieVoteCampaignVM.HallNo;
                    movieVoteCampaign.StartDate = movieVoteCampaignVM.StartDate;
                    movieVoteCampaign.EndDate = movieVoteCampaignVM.EndDate;
                    movieVoteCampaign.IsActive = movieVoteCampaignVM.IsActive;

                    uow.GenericRepository<MovieVoteCampaign>().Update(movieVoteCampaign);
                    uow.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public bool DeleteMovieVoteCampaign(int movieVoteCampaignId)
        {
            try
            {
                MovieVoteCampaign movieVoteCampaign = GetMovieVoteCampaignByID(movieVoteCampaignId);
                if (movieVoteCampaign != null)
                {
                    movieVoteCampaign.IsEnable = false;
                    uow.GenericRepository<MovieVoteCampaign>().Update(movieVoteCampaign);
                    uow.SaveChanges();
                    return true;
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion


        #region Movie Vote Campaign Details

        public List<MovieVoteCampaignDetailVM> GetMovieVoteCampaignDetail(int movieVoteCampaignId)
        {
            return uow.GenericRepository<MovieVoteCampaignDetail>().Table.Where(x => x.IsEnable == true && x.MovieVoteCampaignID == movieVoteCampaignId)
                .Select(y => new MovieVoteCampaignDetailVM
                {
                    MovieTitle = y.Movie.MovieTitle,
                    MovieID = y.MovieID,
                    MovieVoteCampaignID = y.MovieVoteCampaignID,
                    MovieVoteCampaignDetailID = y.MovieVoteCampaignDetailID,
                    Likes = y.MovieVotes.Count(x => x.LikeMovie == true),
                    DisLikes = y.MovieVotes.Count(x => x.LikeMovie == false)
                }).OrderByDescending(x => x.Likes).ToList();
        }

        public List<MoviesVM> GetMov()
        {
            try
            {
                return uow.GenericRepository<Movie>().GetAll().Where(x => x.IsEnable == true).Select
                    (x => new MoviesVM
                    {
                        MovieID = x.MovieID,
                        MovieTitle = x.MovieTitle,
                        MovieDetails = x.MovieDetails,
                        MonthImageUrl = x.MonthImageUrl
                        
                    }).ToList();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        public MovieVoteCampaignDetailVM GetMovieVoteCampaignDetailbyID(int movieVoteCampaignDetailId)
        {
            var camp = uow.GenericRepository<MovieVoteCampaignDetail>().Table.Where(x => x.IsEnable == true && x.MovieVoteCampaignDetailID == movieVoteCampaignDetailId)
                .FirstOrDefault();
            return new MovieVoteCampaignDetailVM
            {
                MovieTitle = camp.Movie.MovieTitle,
                MovieID = camp.MovieID,
                MovieVoteCampaignID = camp.MovieVoteCampaignID,
                MovieVoteCampaignDetailID = camp.MovieVoteCampaignDetailID,
                Likes = camp.MovieVotes.Count(x => x.LikeMovie == true),
                DisLikes = camp.MovieVotes.Count(x => x.LikeMovie == false)
            };
        }

        public MovieVoteCampaignDetail AddMovieVoteCampaignDetail(MovieVoteCampaignDetailVM movieVoteCampaignDetailVM)
        {
            try
            {
                MovieVoteCampaignDetail detail = new MovieVoteCampaignDetail
                {
                    MovieVoteCampaignID = movieVoteCampaignDetailVM.MovieVoteCampaignID,
                    MovieID = movieVoteCampaignDetailVM.MovieID,
                    IsEnable = true,
                    CreatedDate = DateTime.Now
                };

                uow.GenericRepository<MovieVoteCampaignDetail>().Insert(detail);
                uow.SaveChanges();

                return detail;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }


        public bool UpdateMovieVoteCampaignDetail(MovieVoteCampaignDetailVM movieVoteCampaignDetailVM)
        {
            try
            {
                MovieVoteCampaignDetail detail = uow.GenericRepository<MovieVoteCampaignDetail>().GetById(movieVoteCampaignDetailVM.MovieVoteCampaignDetailID);

                detail.MovieID = movieVoteCampaignDetailVM.MovieID;

                uow.GenericRepository<MovieVoteCampaignDetail>().Update(detail);
                uow.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public bool DeleteMovieVoteCampaignDetail(int movieVoteCampaignDetailId)
        {
            try
            {
                MovieVoteCampaignDetail detail = uow.GenericRepository<MovieVoteCampaignDetail>().GetById(movieVoteCampaignDetailId);
                detail.IsEnable = false;
                uow.GenericRepository<MovieVoteCampaignDetail>().Update(detail);
                uow.SaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        #endregion
    }
}
