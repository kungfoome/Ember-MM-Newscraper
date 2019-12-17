﻿' ################################################################################
' #                             EMBER MEDIA MANAGER                              #
' ################################################################################
' ################################################################################
' # This file is part of Ember Media Manager.                                    #
' #                                                                              #
' # Ember Media Manager is free software: you can redistribute it and/or modify  #
' # it under the terms of the GNU General Public License as published by         #
' # the Free Software Foundation, either version 3 of the License, or            #
' # (at your option) any later version.                                          #
' #                                                                              #
' # Ember Media Manager is distributed in the hope that it will be useful,       #
' # but WITHOUT ANY WARRANTY; without even the implied warranty of               #
' # MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the                #
' # GNU General Public License for more details.                                 #
' #                                                                              #
' # You should have received a copy of the GNU General Public License            #
' # along with Ember Media Manager.  If not, see <http://www.gnu.org/licenses/>. #
' ################################################################################

Imports EmberAPI
Imports NLog
Imports System.Diagnostics

Public Class SearchResults_Movie

#Region "Properties"

    Public Property Matches() As New List(Of MediaContainers.Movie)

#End Region 'Properties

End Class

Public Class SearchResults_MovieSet

#Region "Properties"

    Public Property Matches() As New List(Of MediaContainers.MovieSet)

#End Region 'Properties

End Class

Public Class SearchResults_TVShow

#Region "Properties"

    Public Property Matches() As New List(Of MediaContainers.TVShow)

#End Region 'Properties

End Class

Public Class Scraper

#Region "Fields"

    Shared _Logger As Logger = LogManager.GetCurrentClassLogger()

    Private _Client As TMDbLib.Client.TMDbClient  'preferred language
    Private _ClientEN As TMDbLib.Client.TMDbClient 'english language
    Private _AddonSettings As AddonSettings
    Private _Poster As String

    Friend WithEvents bwTMDB As New ComponentModel.BackgroundWorker

#End Region 'Fields

#Region "Properties"

    Public Property DefaultLanguage As String
        Get
            Return _Client.DefaultLanguage
        End Get
        Set(value As String)
            _Client.DefaultLanguage = value
        End Set
    End Property

    Public ReadOnly Property IsClientCreated As Boolean
        Get
            Return _Client IsNot Nothing
        End Get
    End Property

#End Region 'Properties

#Region "Enumerations"

    Private Enum SearchType
        Movies
        Details
        SearchDetails_Movie
        MovieSets
        SearchDetails_MovieSet
        TVShows
        SearchDetails_TVShow
    End Enum

#End Region 'Enumerations

#Region "Events"

    Public Event SearchInfoDownloaded_Movie(ByVal strPoster As String, ByVal sInfo As MediaContainers.Movie)
    Public Event SearchInfoDownloaded_MovieSet(ByVal strPoster As String, ByVal sInfo As MediaContainers.MovieSet)
    Public Event SearchInfoDownloaded_TVShow(ByVal strPoster As String, ByVal sInfo As MediaContainers.TVShow)

    Public Event SearchResultsDownloaded_Movie(ByVal mResults As SearchResults_Movie)
    Public Event SearchResultsDownloaded_MovieSet(ByVal mResults As SearchResults_MovieSet)
    Public Event SearchResultsDownloaded_TVShow(ByVal mResults As SearchResults_TVShow)

#End Region 'Events

#Region "Methods"

    Public Async Function CreateAPI(ByVal AddonSettings As AddonSettings) As Task
        Try
            _AddonSettings = AddonSettings

            _Client = New TMDbLib.Client.TMDbClient(_AddonSettings.APIKey)
            Await _Client.GetConfigAsync()
            _Client.MaxRetryCount = 2
            _Logger.Trace("[TMDB_Data] [CreateAPI] Client created")

            If _AddonSettings.FallBackEng Then
                _ClientEN = New TMDbLib.Client.TMDbClient(_AddonSettings.APIKey)
                Await _ClientEN.GetConfigAsync()
                _ClientEN.DefaultLanguage = "en-US"
                _ClientEN.MaxRetryCount = 2
                _Logger.Trace("[TMDB_Data] [CreateAPI] Client-EN created")
            Else
                _ClientEN = _Client
                _Logger.Trace("[TMDB_Data] [CreateAPI] Client-EN = Client")
            End If
        Catch ex As Exception
            _Logger.Error(String.Format("[TMDB_Data] [CreateAPI] [Error] {0}", ex.Message))
        End Try
    End Function

    Private Sub bwTMDB_DoWork(ByVal sender As Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles bwTMDB.DoWork
        Dim Args As Arguments = DirectCast(e.Argument, Arguments)
        '' The rule is that if there is a tt is an IMDB otherwise is a TMDB

        Select Case Args.Search
            Case SearchType.Movies
                Dim r As SearchResults_Movie = SearchMovie(Args.Parameter, Args.Year)
                e.Result = New Results With {.ResultType = SearchType.Movies, .Result = r}

            Case SearchType.MovieSets
                Dim r As SearchResults_MovieSet = SearchMovieSet(Args.Parameter)
                e.Result = New Results With {.ResultType = SearchType.MovieSets, .Result = r}

            Case SearchType.TVShows
                Dim r As SearchResults_TVShow = SearchTVShow(Args.Parameter)
                e.Result = New Results With {.ResultType = SearchType.TVShows, .Result = r}

            Case SearchType.SearchDetails_Movie
                Dim r As MediaContainers.Movie = GetInfo_Movie(Args.Parameter, Args.ScrapeOptions, True)
                e.Result = New Results With {.ResultType = SearchType.SearchDetails_Movie, .Result = r}

            Case SearchType.SearchDetails_MovieSet
                Dim r As MediaContainers.MovieSet = GetInfo_MovieSet(Args.Parameter, Args.ScrapeOptions, True)
                e.Result = New Results With {.ResultType = SearchType.SearchDetails_MovieSet, .Result = r}

            Case SearchType.SearchDetails_TVShow
                Dim r As MediaContainers.TVShow = GetInfo_TVShow(Args.Parameter, Args.ScrapeModifiers, Args.ScrapeOptions, True)
                e.Result = New Results With {.ResultType = SearchType.SearchDetails_TVShow, .Result = r}
        End Select
    End Sub

    Private Sub bwTMDB_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles bwTMDB.RunWorkerCompleted
        Dim Res As Results = DirectCast(e.Result, Results)

        Select Case Res.ResultType
            Case SearchType.Movies
                RaiseEvent SearchResultsDownloaded_Movie(DirectCast(Res.Result, SearchResults_Movie))

            Case SearchType.MovieSets
                RaiseEvent SearchResultsDownloaded_MovieSet(DirectCast(Res.Result, SearchResults_MovieSet))

            Case SearchType.TVShows
                RaiseEvent SearchResultsDownloaded_TVShow(DirectCast(Res.Result, SearchResults_TVShow))

            Case SearchType.SearchDetails_Movie
                Dim movieInfo As MediaContainers.Movie = DirectCast(Res.Result, MediaContainers.Movie)
                RaiseEvent SearchInfoDownloaded_Movie(_Poster, movieInfo)

            Case SearchType.SearchDetails_MovieSet
                Dim moviesetInfo As MediaContainers.MovieSet = DirectCast(Res.Result, MediaContainers.MovieSet)
                RaiseEvent SearchInfoDownloaded_MovieSet(_Poster, moviesetInfo)

            Case SearchType.SearchDetails_TVShow
                Dim showInfo As MediaContainers.TVShow = DirectCast(Res.Result, MediaContainers.TVShow)
                RaiseEvent SearchInfoDownloaded_TVShow(_Poster, showInfo)
        End Select
    End Sub

    Public Sub CancelAsync()
        If bwTMDB.IsBusy Then bwTMDB.CancelAsync()

        While bwTMDB.IsBusy
            Application.DoEvents()
            Threading.Thread.Sleep(50)
        End While
    End Sub

    Public Function GetImages_Movie_MovieSet(ByVal TMDbID As String, ByVal FilteredModifiers As Structures.ScrapeModifiers, ByVal ContentType As Enums.ContentType) As MediaContainers.SearchResultsContainer
        Dim alImagesContainer As New MediaContainers.SearchResultsContainer

        Try
            Dim Results As TMDbLib.Objects.General.Images = Nothing
            Dim APIResult As Task(Of TMDbLib.Objects.General.ImagesWithId)

            If ContentType = Enums.ContentType.Movie Then
                APIResult = Task.Run(Function() _Client.GetMovieImagesAsync(CInt(TMDbID)))
                Results = APIResult.Result
            ElseIf ContentType = Enums.ContentType.Movieset Then
                APIResult = Task.Run(Function() _Client.GetCollectionImagesAsync(CInt(TMDbID)))
                Results = APIResult.Result
            End If

            If Results Is Nothing Then
                Return alImagesContainer
            End If

            'MainFanart
            If FilteredModifiers.MainFanart AndAlso Results.Backdrops IsNot Nothing Then
                For Each tImage In Results.Backdrops
                    Dim newImage As New MediaContainers.Image With {
                            .Height = tImage.Height,
                            .Likes = 0,
                            .Scraper = "TMDB",
                            .Language = If(String.IsNullOrEmpty(tImage.Iso_639_1), String.Empty, tImage.Iso_639_1),
                            .URLOriginal = _Client.Config.Images.BaseUrl & "original" & tImage.FilePath,
                            .URLThumb = _Client.Config.Images.BaseUrl & "w300" & tImage.FilePath,
                            .VoteAverage = tImage.VoteAverage,
                            .VoteCount = tImage.VoteCount,
                            .Width = tImage.Width}

                    alImagesContainer.MainFanarts.Add(newImage)
                Next
            End If

            'MainPoster
            If FilteredModifiers.MainPoster AndAlso Results.Posters IsNot Nothing Then
                For Each tImage In Results.Posters
                    Dim newImage As New MediaContainers.Image With {
                                .Height = tImage.Height,
                                .Likes = 0,
                                .Scraper = "TMDB",
                                .Language = If(String.IsNullOrEmpty(tImage.Iso_639_1), String.Empty, tImage.Iso_639_1),
                                .URLOriginal = _Client.Config.Images.BaseUrl & "original" & tImage.FilePath,
                                .URLThumb = _Client.Config.Images.BaseUrl & "w185" & tImage.FilePath,
                                .VoteAverage = tImage.VoteAverage,
                                .VoteCount = tImage.VoteCount,
                                .Width = tImage.Width}

                    alImagesContainer.MainPosters.Add(newImage)
                Next
            End If

        Catch ex As Exception
            _Logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try

        Return alImagesContainer
    End Function

    Public Function GetImages_TV(ByVal tmdbID As String, ByVal FilteredModifiers As Structures.ScrapeModifiers) As MediaContainers.SearchResultsContainer
        Dim alImagesContainer As New MediaContainers.SearchResultsContainer

        Try
            Dim APIResult As Task(Of TMDbLib.Objects.TvShows.TvShow)
            APIResult = Task.Run(Function() _Client.GetTvShowAsync(CInt(tmdbID), TMDbLib.Objects.TvShows.TvShowMethods.Images))

            If APIResult Is Nothing Then
                Return alImagesContainer
            End If

            Dim Result As TMDbLib.Objects.TvShows.TvShow = APIResult.Result

            'MainFanart
            If FilteredModifiers.MainFanart AndAlso Result.Images.Backdrops IsNot Nothing Then
                For Each tImage In Result.Images.Backdrops
                    Dim newImage As New MediaContainers.Image With {
                            .Height = tImage.Height,
                            .Likes = 0,
                            .Scraper = "TMDB",
                            .Language = If(String.IsNullOrEmpty(tImage.Iso_639_1), String.Empty, tImage.Iso_639_1),
                            .URLOriginal = _Client.Config.Images.BaseUrl & "original" & tImage.FilePath,
                            .URLThumb = _Client.Config.Images.BaseUrl & "w300" & tImage.FilePath,
                            .VoteAverage = tImage.VoteAverage,
                            .VoteCount = tImage.VoteCount,
                            .Width = tImage.Width}

                    alImagesContainer.MainFanarts.Add(newImage)
                Next
            End If

            'MainPoster
            If FilteredModifiers.MainPoster AndAlso Result.Images.Posters IsNot Nothing Then
                For Each tImage In Result.Images.Posters
                    Dim newImage As New MediaContainers.Image With {
                                .Height = tImage.Height,
                                .Likes = 0,
                                .Scraper = "TMDB",
                                .Language = If(String.IsNullOrEmpty(tImage.Iso_639_1), String.Empty, tImage.Iso_639_1),
                                .URLOriginal = _Client.Config.Images.BaseUrl & "original" & tImage.FilePath,
                                .URLThumb = _Client.Config.Images.BaseUrl & "w185" & tImage.FilePath,
                                .VoteAverage = tImage.VoteAverage,
                                .VoteCount = tImage.VoteCount,
                                .Width = tImage.Width}

                    alImagesContainer.MainPosters.Add(newImage)
                Next
            End If

            'SeasonPoster
            If (FilteredModifiers.SeasonPoster OrElse FilteredModifiers.EpisodePoster) AndAlso Result.Seasons IsNot Nothing Then
                For Each tSeason In Result.Seasons
                    Dim APIResult_Season As Task(Of TMDbLib.Objects.TvShows.TvSeason)
                    APIResult_Season = Task.Run(Function() _Client.GetTvSeasonAsync(CInt(tmdbID), tSeason.SeasonNumber, TMDbLib.Objects.TvShows.TvSeasonMethods.Images))

                    If APIResult_Season IsNot Nothing Then
                        Dim Result_Season As TMDbLib.Objects.TvShows.TvSeason = APIResult_Season.Result

                        'SeasonPoster
                        If FilteredModifiers.SeasonPoster AndAlso Result_Season.Images.Posters IsNot Nothing Then
                            For Each tImage In Result_Season.Images.Posters
                                Dim newImage As New MediaContainers.Image With {
                                        .Height = tImage.Height,
                                        .Likes = 0,
                                        .Scraper = "TMDB",
                                        .Season = tSeason.SeasonNumber,
                                        .Language = If(String.IsNullOrEmpty(tImage.Iso_639_1), String.Empty, tImage.Iso_639_1),
                                        .URLOriginal = _Client.Config.Images.BaseUrl & "original" & tImage.FilePath,
                                        .URLThumb = _Client.Config.Images.BaseUrl & "w185" & tImage.FilePath,
                                        .VoteAverage = tImage.VoteAverage,
                                        .VoteCount = tImage.VoteCount,
                                        .Width = tImage.Width}

                                alImagesContainer.SeasonPosters.Add(newImage)
                            Next
                        End If

                        If FilteredModifiers.EpisodePoster AndAlso Result_Season.Episodes IsNot Nothing Then
                            For Each tEpisode In Result_Season.Episodes

                                'EpisodePoster
                                If FilteredModifiers.EpisodePoster AndAlso tEpisode.StillPath IsNot Nothing Then

                                    Dim newImage As New MediaContainers.Image With {
                                            .Episode = tEpisode.EpisodeNumber,
                                            .Scraper = "TMDB",
                                            .Season = CInt(tEpisode.SeasonNumber),
                                            .URLOriginal = _Client.Config.Images.BaseUrl & "original" & tEpisode.StillPath,
                                            .URLThumb = _Client.Config.Images.BaseUrl & "w185" & tEpisode.StillPath}

                                    alImagesContainer.EpisodePosters.Add(newImage)
                                End If
                            Next
                        End If
                    End If
                Next
            End If

        Catch ex As Exception
            _Logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try

        Return alImagesContainer
    End Function

    Public Function GetImages_TVEpisode(ByVal tmdbID As String, ByVal iSeason As Integer, ByVal iEpisode As Integer, ByVal FilteredModifiers As Structures.ScrapeModifiers) As MediaContainers.SearchResultsContainer
        Dim alImagesContainer As New MediaContainers.SearchResultsContainer

        Try
            Dim Results As TMDbLib.Objects.General.StillImages = Nothing
            Dim APIResult As Task(Of TMDbLib.Objects.General.StillImages)
            APIResult = Task.Run(Function() _Client.GetTvEpisodeImagesAsync(CInt(tmdbID), iSeason, iEpisode))
            Results = APIResult.Result

            If Results Is Nothing Then
                Return alImagesContainer
            End If

            'EpisodePoster
            If FilteredModifiers.EpisodePoster AndAlso Results.Stills IsNot Nothing Then
                For Each tImage In Results.Stills
                    Dim newImage As New MediaContainers.Image With {
                            .Episode = iEpisode,
                            .Height = tImage.Height,
                            .Likes = 0,
                            .Scraper = "TMDB",
                            .Season = iSeason,
                            .Language = If(String.IsNullOrEmpty(tImage.Iso_639_1), String.Empty, tImage.Iso_639_1),
                            .URLOriginal = _Client.Config.Images.BaseUrl & "original" & tImage.FilePath,
                            .URLThumb = _Client.Config.Images.BaseUrl & "w185" & tImage.FilePath,
                            .VoteAverage = tImage.VoteAverage,
                            .VoteCount = tImage.VoteCount,
                            .Width = tImage.Width}

                    alImagesContainer.EpisodePosters.Add(newImage)
                Next
            End If

        Catch ex As Exception
            _Logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try

        Return alImagesContainer
    End Function

    Public Function GetMovieID(ByVal IMDbID As String) As String
        Dim Movie As TMDbLib.Objects.Movies.Movie

        Dim APIResult As Task(Of TMDbLib.Objects.Movies.Movie)
        APIResult = Task.Run(Function() _Client.GetMovieAsync(IMDbID))

        Movie = APIResult.Result
        If Movie Is Nothing OrElse Movie.Id = 0 Then Return String.Empty

        Return CStr(Movie.Id)
    End Function

    Public Function GetMovieCollectionID(ByVal IMDbID As String) As String
        Dim Movie As TMDbLib.Objects.Movies.Movie

        Dim APIResult As Task(Of TMDbLib.Objects.Movies.Movie)
        APIResult = Task.Run(Function() _Client.GetMovieAsync(IMDbID))

        Movie = APIResult.Result
        If Movie Is Nothing Then Return String.Empty

        If Movie.BelongsToCollection IsNot Nothing AndAlso Movie.BelongsToCollection.Id > 0 Then
            Return CStr(Movie.BelongsToCollection.Id)
        Else
            Return String.Empty
        End If
    End Function
    ''' <summary>
    '''  Scrape MovieDetails from TMDB
    ''' </summary>
    ''' <param name="ID">TMDBID or ID (IMDB ID starts with "tt") of movie to be scraped</param>
    ''' <param name="GetPoster">Scrape posters for the movie?</param>
    ''' <returns>True: success, false: no success</returns>
    Public Function GetInfo_Movie(ByVal ID As String, ByVal FilteredOptions As Structures.ScrapeOptions, ByVal GetPoster As Boolean) As MediaContainers.Movie
        If String.IsNullOrEmpty(ID) Then Return Nothing

        Dim nMovie As New MediaContainers.Movie
        Dim intTMDBID As Integer = -1

        If bwTMDB.CancellationPending Then Return Nothing

        Dim APIResult As Task(Of TMDbLib.Objects.Movies.Movie)
        Dim APIResultE As Task(Of TMDbLib.Objects.Movies.Movie)

        If ID.ToLower.StartsWith("tt") Then
            'search movie by IMDB ID
            APIResult = Task.Run(Function() _Client.GetMovieAsync(ID, TMDbLib.Objects.Movies.MovieMethods.Credits Or TMDbLib.Objects.Movies.MovieMethods.Releases Or TMDbLib.Objects.Movies.MovieMethods.Videos))
            If _AddonSettings.FallBackEng Then
                APIResultE = Task.Run(Function() _ClientEN.GetMovieAsync(ID, TMDbLib.Objects.Movies.MovieMethods.Credits Or TMDbLib.Objects.Movies.MovieMethods.Releases Or TMDbLib.Objects.Movies.MovieMethods.Videos))
            Else
                APIResultE = APIResult
            End If
        ElseIf Integer.TryParse(ID, intTMDBID) Then
            'search movie by TMDB ID
            APIResult = Task.Run(Function() _Client.GetMovieAsync(intTMDBID, TMDbLib.Objects.Movies.MovieMethods.Credits Or TMDbLib.Objects.Movies.MovieMethods.Releases Or TMDbLib.Objects.Movies.MovieMethods.Videos))
            If _AddonSettings.FallBackEng Then
                APIResultE = Task.Run(Function() _ClientEN.GetMovieAsync(intTMDBID, TMDbLib.Objects.Movies.MovieMethods.Credits Or TMDbLib.Objects.Movies.MovieMethods.Releases Or TMDbLib.Objects.Movies.MovieMethods.Videos))
            Else
                APIResultE = APIResult
            End If
        Else
            Return Nothing
        End If

        Dim Result As TMDbLib.Objects.Movies.Movie = APIResult.Result
        Dim ResultE As TMDbLib.Objects.Movies.Movie = APIResultE.Result

        If (Result Is Nothing AndAlso Not _AddonSettings.FallBackEng) OrElse (Result Is Nothing AndAlso ResultE Is Nothing) OrElse
                (Not Result.Id > 0 AndAlso Not _AddonSettings.FallBackEng) OrElse (Not Result.Id > 0 AndAlso Not ResultE.Id > 0) Then
            _Logger.Error(String.Format("Can't scrape or movie not found: [0]", ID))
            Return Nothing
        End If

        nMovie.Scrapersource = "TMDB"

        'IDs
        nMovie.UniqueIDs.TMDbId = CStr(Result.Id)
        If Result.ImdbId IsNot Nothing Then nMovie.UniqueIDs.IMDbId = Result.ImdbId

        If bwTMDB.CancellationPending Or Result Is Nothing Then Return Nothing

        'Cast (Actors)
        If FilteredOptions.bMainActors Then
            If Result.Credits IsNot Nothing AndAlso Result.Credits.Cast IsNot Nothing Then
                For Each aCast As TMDbLib.Objects.Movies.Cast In Result.Credits.Cast
                    Dim nUniqueID As New MediaContainers.Uniqueid With {
                        .Type = "tmdb",
                        .Value = aCast.Id.ToString}
                    nMovie.Actors.Add(New MediaContainers.Person With {
                                      .Name = aCast.Name,
                                      .Role = aCast.Character,
                                      .URLOriginal = If(Not String.IsNullOrEmpty(aCast.ProfilePath), String.Concat(_Client.Config.Images.BaseUrl, "original", aCast.ProfilePath), String.Empty),
                                      .UniqueIDs = New MediaContainers.UniqueidContainer With {.Items = New List(Of MediaContainers.Uniqueid)(New MediaContainers.Uniqueid() {nUniqueID})}
                                      })
                Next
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Certifications
        If FilteredOptions.bMainCertifications Then
            If Result.Releases IsNot Nothing AndAlso Result.Releases.Countries IsNot Nothing AndAlso Result.Releases.Countries.Count > 0 Then
                For Each cCountry In Result.Releases.Countries
                    If Not String.IsNullOrEmpty(cCountry.Certification) Then
                        Dim CertificationLanguage = APIXML.CertificationLanguages.Language.FirstOrDefault(Function(l) l.abbreviation = cCountry.Iso_3166_1.ToLower)
                        If CertificationLanguage IsNot Nothing AndAlso CertificationLanguage.name IsNot Nothing AndAlso Not String.IsNullOrEmpty(CertificationLanguage.name) Then
                            nMovie.Certifications.Add(String.Concat(CertificationLanguage.name, ":", cCountry.Certification))
                        Else
                            _Logger.Warn("Unhandled certification language encountered: {0}", cCountry.Iso_3166_1.ToLower)
                        End If
                    End If
                Next
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Collection ID
        If FilteredOptions.bMainCollectionID Then
            If Result.BelongsToCollection Is Nothing Then
                If _AddonSettings.FallBackEng AndAlso ResultE.BelongsToCollection IsNot Nothing Then
                    Dim nFullMovieSetInfo = GetInfo_MovieSet(ResultE.BelongsToCollection.Id.ToString,
                                                                 New Structures.ScrapeOptions With {.bMainPlot = True, .bMainTitle = True},
                                                                 False)
                    nMovie.AddSet(New MediaContainers.SetDetails With {
                                      .ID = -1,
                                      .Order = -1,
                                      .Plot = nFullMovieSetInfo.Plot,
                                      .Title = nFullMovieSetInfo.Title,
                                      .TMDbId = nFullMovieSetInfo.UniqueIDs.TMDbId})
                    nMovie.UniqueIDs.TMDbCollectionId = nFullMovieSetInfo.UniqueIDs.TMDbId
                End If
            Else
                Dim nFullMovieSetInfo = GetInfo_MovieSet(Result.BelongsToCollection.Id.ToString,
                                                             New Structures.ScrapeOptions With {.bMainPlot = True, .bMainTitle = True},
                                                             False)
                nMovie.AddSet(New MediaContainers.SetDetails With {
                                  .ID = -1,
                                  .Order = -1,
                                  .Plot = nFullMovieSetInfo.Plot,
                                  .Title = nFullMovieSetInfo.Title,
                                  .TMDbId = nFullMovieSetInfo.UniqueIDs.TMDbId})
                nMovie.UniqueIDs.TMDbCollectionId = nFullMovieSetInfo.UniqueIDs.TMDbId
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Countries
        If FilteredOptions.bMainCountries Then
            If Result.ProductionCountries IsNot Nothing AndAlso Result.ProductionCountries.Count > 0 Then
                For Each aContry As TMDbLib.Objects.Movies.ProductionCountry In Result.ProductionCountries
                    nMovie.Countries.Add(aContry.Name)
                Next
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Director / Writer
        If FilteredOptions.bMainDirectors OrElse FilteredOptions.bMainWriters Then
            If Result.Credits IsNot Nothing AndAlso Result.Credits.Crew IsNot Nothing Then
                For Each aCrew As TMDbLib.Objects.General.Crew In Result.Credits.Crew
                    If FilteredOptions.bMainDirectors AndAlso aCrew.Department = "Directing" AndAlso aCrew.Job = "Director" Then
                        nMovie.Directors.Add(aCrew.Name)
                    End If
                    If FilteredOptions.bMainWriters AndAlso aCrew.Department = "Writing" AndAlso (aCrew.Job = "Author" OrElse aCrew.Job = "Screenplay" OrElse aCrew.Job = "Writer") Then
                        nMovie.Credits.Add(aCrew.Name)
                    End If
                Next
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Genres
        If FilteredOptions.bMainGenres Then
            Dim aGenres As List(Of TMDbLib.Objects.General.Genre) = Nothing
            If Result.Genres Is Nothing OrElse (Result.Genres IsNot Nothing AndAlso Result.Genres.Count = 0) Then
                If _AddonSettings.FallBackEng AndAlso ResultE.Genres IsNot Nothing AndAlso ResultE.Genres.Count > 0 Then
                    aGenres = ResultE.Genres
                End If
            Else
                aGenres = Result.Genres
            End If

            If aGenres IsNot Nothing Then
                For Each tGenre As TMDbLib.Objects.General.Genre In aGenres
                    nMovie.Genres.Add(tGenre.Name)
                Next
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'OriginalTitle
        If FilteredOptions.bMainOriginalTitle Then
            If Result.OriginalTitle Is Nothing OrElse (Result.OriginalTitle IsNot Nothing AndAlso String.IsNullOrEmpty(Result.OriginalTitle)) Then
                If _AddonSettings.FallBackEng AndAlso ResultE.OriginalTitle IsNot Nothing AndAlso Not String.IsNullOrEmpty(ResultE.OriginalTitle) Then
                    nMovie.OriginalTitle = ResultE.OriginalTitle
                End If
            Else
                nMovie.OriginalTitle = Result.OriginalTitle
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Plot
        If FilteredOptions.bMainPlot Then
            If Result.Overview Is Nothing OrElse (Result.Overview IsNot Nothing AndAlso String.IsNullOrEmpty(Result.Overview)) Then
                If _AddonSettings.FallBackEng AndAlso ResultE.Overview IsNot Nothing AndAlso Not String.IsNullOrEmpty(ResultE.Overview) Then
                    nMovie.Plot = ResultE.Overview
                End If
            Else
                nMovie.Plot = Result.Overview
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Posters (only for SearchResult dialog, auto fallback to "en" by TMDB)
        If GetPoster Then
            If Result.PosterPath IsNot Nothing AndAlso Not String.IsNullOrEmpty(Result.PosterPath) Then
                _Poster = String.Concat(_Client.Config.Images.BaseUrl, "w92", Result.PosterPath)
            Else
                _Poster = String.Empty
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Rating
        If FilteredOptions.bMainRating Then
            nMovie.Ratings.Add(New MediaContainers.RatingDetails With {.Max = 10, .Name = "themoviedb", .Value = Result.VoteAverage, .Votes = Result.VoteCount})
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'ReleaseDate
        If FilteredOptions.bMainRelease Then
            Dim ScrapedDate As String = String.Empty
            If Result.ReleaseDate Is Nothing OrElse (Result.ReleaseDate IsNot Nothing AndAlso String.IsNullOrEmpty(CStr(Result.ReleaseDate))) Then
                If _AddonSettings.FallBackEng AndAlso ResultE.ReleaseDate IsNot Nothing AndAlso Not String.IsNullOrEmpty(CStr(ResultE.ReleaseDate)) Then
                    ScrapedDate = CStr(ResultE.ReleaseDate)
                End If
            Else
                ScrapedDate = CStr(Result.ReleaseDate)
            End If
            If Not String.IsNullOrEmpty(ScrapedDate) Then
                Dim RelDate As Date
                If Date.TryParse(ScrapedDate, RelDate) Then
                    'always save date in same date format not depending on users language setting!
                    nMovie.ReleaseDate = RelDate.ToString("yyyy-MM-dd")
                Else
                    nMovie.ReleaseDate = ScrapedDate
                End If
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Runtime
        If FilteredOptions.bMainRuntime Then
            If Result.Runtime Is Nothing OrElse Result.Runtime = 0 Then
                If _AddonSettings.FallBackEng AndAlso ResultE.Runtime IsNot Nothing Then
                    nMovie.Runtime = CStr(ResultE.Runtime)
                End If
            Else
                nMovie.Runtime = CStr(Result.Runtime)
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Studios
        If FilteredOptions.bMainStudios Then
            If Result.ProductionCompanies IsNot Nothing AndAlso Result.ProductionCompanies.Count > 0 Then
                For Each cStudio In Result.ProductionCompanies
                    nMovie.Studios.Add(cStudio.Name)
                Next
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Tagline
        If FilteredOptions.bMainTagline Then
            If Result.Tagline Is Nothing OrElse (Result.Tagline IsNot Nothing AndAlso String.IsNullOrEmpty(Result.Tagline)) Then
                If _AddonSettings.FallBackEng AndAlso ResultE.Tagline IsNot Nothing AndAlso Not String.IsNullOrEmpty(ResultE.Tagline) Then
                    nMovie.Tagline = ResultE.Tagline
                End If
            Else
                nMovie.Tagline = Result.Tagline
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Title
        If FilteredOptions.bMainTitle Then
            If Result.Title Is Nothing OrElse (Result.Title IsNot Nothing AndAlso String.IsNullOrEmpty(Result.Title)) Then
                If _AddonSettings.FallBackEng AndAlso ResultE.Title IsNot Nothing AndAlso Not String.IsNullOrEmpty(ResultE.Title) Then
                    nMovie.Title = ResultE.Title
                End If
            Else
                nMovie.Title = Result.Title
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Trailer
        If FilteredOptions.bMainTrailer Then
            Dim aTrailers As List(Of TMDbLib.Objects.General.Video) = Nothing
            If Result.Videos Is Nothing OrElse (Result.Videos IsNot Nothing AndAlso Result.Videos.Results.Count = 0) Then
                If _AddonSettings.FallBackEng AndAlso ResultE.Videos IsNot Nothing AndAlso ResultE.Videos.Results.Count > 0 Then
                    aTrailers = ResultE.Videos.Results
                End If
            Else
                aTrailers = Result.Videos.Results
            End If

            If aTrailers IsNot Nothing AndAlso aTrailers.Count > 0 Then
                For Each tTrailer In aTrailers
                    If YouTube.Scraper.IsAvailable("http://www.youtube.com/watch?hd=1&v=" & tTrailer.Key) Then
                        nMovie.Trailer = "http://www.youtube.com/watch?hd=1&v=" & tTrailer.Key
                        Exit For
                    End If
                Next
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        'Year
        If FilteredOptions.bMainYear Then
            If Result.ReleaseDate Is Nothing OrElse (Result.ReleaseDate IsNot Nothing AndAlso String.IsNullOrEmpty(CStr(Result.ReleaseDate))) Then
                If _AddonSettings.FallBackEng AndAlso ResultE.ReleaseDate IsNot Nothing AndAlso Not String.IsNullOrEmpty(CStr(ResultE.ReleaseDate)) Then
                    nMovie.Year = ResultE.ReleaseDate.Value.Year
                End If
            Else
                nMovie.Year = Result.ReleaseDate.Value.Year
            End If
        End If

        If bwTMDB.CancellationPending Then Return Nothing

        Return nMovie
    End Function

    Public Function GetInfo_MovieSet(ByVal ID As String, ByVal FilteredOptions As Structures.ScrapeOptions, ByVal GetPoster As Boolean) As MediaContainers.MovieSet
        If String.IsNullOrEmpty(ID) OrElse Not Integer.TryParse(ID, 0) Then Return Nothing

        Dim nMovieSet As New MediaContainers.MovieSet
        Dim intTMDBID As Integer = -1

        If Integer.TryParse(ID, intTMDBID) Then
            If bwTMDB.CancellationPending Then Return Nothing

            Dim APIResult As Task(Of TMDbLib.Objects.Collections.Collection)
            Dim APIResultE As Task(Of TMDbLib.Objects.Collections.Collection)

            APIResult = Task.Run(Function() _Client.GetCollectionAsync(intTMDBID))
            If _AddonSettings.FallBackEng Then
                APIResultE = Task.Run(Function() _ClientEN.GetCollectionAsync(intTMDBID))
            Else
                APIResultE = APIResult
            End If

            If APIResult Is Nothing OrElse APIResultE Is Nothing Then
                Return Nothing
            End If

            Dim Result As TMDbLib.Objects.Collections.Collection = APIResult.Result
            Dim ResultE As TMDbLib.Objects.Collections.Collection = APIResultE.Result

            If (Result Is Nothing AndAlso Not _AddonSettings.FallBackEng) OrElse (Result Is Nothing AndAlso ResultE Is Nothing) OrElse
                (Not Result.Id > 0 AndAlso Not _AddonSettings.FallBackEng) OrElse (Not Result.Id > 0 AndAlso Not ResultE.Id > 0) Then
                _Logger.Warn(String.Format("[TMDB_Data] [Abort] No API result for TMDB Collection ID [{0}]", ID))
                Return Nothing
            End If

            nMovieSet.UniqueIDs.TMDbId = CStr(Result.Id)

            If bwTMDB.CancellationPending Or Result Is Nothing Then Return Nothing

            'Plot
            If FilteredOptions.bMainPlot Then
                If Result.Overview Is Nothing OrElse (Result.Overview IsNot Nothing AndAlso String.IsNullOrEmpty(Result.Overview)) Then
                    If _AddonSettings.FallBackEng AndAlso ResultE.Overview IsNot Nothing AndAlso Not String.IsNullOrEmpty(ResultE.Overview) Then
                        nMovieSet.Plot = HttpUtility.HtmlDecode(ResultE.Overview)
                    End If
                Else
                    nMovieSet.Plot = HttpUtility.HtmlDecode(Result.Overview)
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Posters (only for SearchResult dialog, auto fallback to "en" by TMDB)
            If GetPoster Then
                If Result.PosterPath IsNot Nothing AndAlso Not String.IsNullOrEmpty(Result.PosterPath) Then
                    _Poster = String.Concat(_Client.Config.Images.BaseUrl, "w92", Result.PosterPath)
                Else
                    _Poster = String.Empty
                End If
            End If

            'Title
            If FilteredOptions.bMainTitle Then
                If Result.Name Is Nothing OrElse (Result.Name IsNot Nothing AndAlso String.IsNullOrEmpty(Result.Name)) Then
                    If _AddonSettings.FallBackEng AndAlso ResultE.Name IsNot Nothing AndAlso Not String.IsNullOrEmpty(ResultE.Name) Then
                        'nMovieSet.Title = MovieSetE.Name
                        nMovieSet.Title = ResultE.Name
                    End If
                Else
                    'nMovieSet.Title = MovieSet.Name
                    nMovieSet.Title = Result.Name
                End If
            End If
        Else
            Return Nothing
        End If

        Return nMovieSet
    End Function
    ''' <summary>
    '''  Scrape TV Show details from TMDB
    ''' </summary>
    ''' <param name="strID">TMDB ID of tv show to be scraped</param>
    ''' <param name="GetPoster">Scrape posters for the movie?</param>
    ''' <returns>True: success, false: no success</returns>
    Public Function GetInfo_TVShow(ByVal strID As String, ByRef ScrapeModifiers As Structures.ScrapeModifiers, ByRef FilteredOptions As Structures.ScrapeOptions, ByVal GetPoster As Boolean) As MediaContainers.TVShow
        If String.IsNullOrEmpty(strID) Then Return Nothing

        Dim nTVShow As New MediaContainers.TVShow
        Dim intTMDBID As Integer = -1

        If Integer.TryParse(strID, intTMDBID) Then
            If bwTMDB.CancellationPending Then Return Nothing

            Dim APIResult As Task(Of TMDbLib.Objects.TvShows.TvShow)
            Dim APIResultE As Task(Of TMDbLib.Objects.TvShows.TvShow)

            'search movie by TMDB ID
            APIResult = Task.Run(Function() _Client.GetTvShowAsync(CInt(intTMDBID), TMDbLib.Objects.TvShows.TvShowMethods.ContentRatings Or TMDbLib.Objects.TvShows.TvShowMethods.Credits Or TMDbLib.Objects.TvShows.TvShowMethods.ExternalIds))
            If _AddonSettings.FallBackEng Then
                APIResultE = Task.Run(Function() _ClientEN.GetTvShowAsync(CInt(intTMDBID), TMDbLib.Objects.TvShows.TvShowMethods.ContentRatings Or TMDbLib.Objects.TvShows.TvShowMethods.Credits Or TMDbLib.Objects.TvShows.TvShowMethods.ExternalIds))
            Else
                APIResultE = APIResult
            End If

            If APIResult Is Nothing OrElse APIResultE Is Nothing Then
                Return Nothing
            End If

            Dim Result As TMDbLib.Objects.TvShows.TvShow = APIResult.Result
            Dim ResultE As TMDbLib.Objects.TvShows.TvShow = APIResultE.Result

            If (Result Is Nothing AndAlso Not _AddonSettings.FallBackEng) OrElse (Result Is Nothing AndAlso ResultE Is Nothing) OrElse
                    (Not Result.Id > 0 AndAlso Not _AddonSettings.FallBackEng) OrElse (Not Result.Id > 0 AndAlso Not ResultE.Id > 0) Then
                _Logger.Error(String.Format("Can't scrape or tv show not found: [{0}]", strID))
                Return Nothing
            End If

            nTVShow.Scrapersource = "TMDB"

            'IDs
            nTVShow.UniqueIDs.TMDbId = CStr(Result.Id)
            If Result.ExternalIds.TvdbId IsNot Nothing Then nTVShow.UniqueIDs.TVDbId = Result.ExternalIds.TvdbId
            If Result.ExternalIds.ImdbId IsNot Nothing Then nTVShow.UniqueIDs.IMDbId = Result.ExternalIds.ImdbId

            If bwTMDB.CancellationPending Or Result Is Nothing Then Return Nothing

            'Actors
            If FilteredOptions.bMainActors Then
                If Result.Credits IsNot Nothing AndAlso Result.Credits.Cast IsNot Nothing Then
                    For Each aCast As TMDbLib.Objects.TvShows.Cast In Result.Credits.Cast
                        Dim nUniqueID As New MediaContainers.Uniqueid With {
                            .Type = "tmdb",
                            .Value = aCast.Id.ToString}
                        nTVShow.Actors.Add(New MediaContainers.Person With {
                                           .Name = aCast.Name,
                                           .Role = aCast.Character,
                                           .URLOriginal = If(Not String.IsNullOrEmpty(aCast.ProfilePath), String.Concat(_Client.Config.Images.BaseUrl, "original", aCast.ProfilePath), String.Empty),
                                           .UniqueIDs = New MediaContainers.UniqueidContainer With {.Items = New List(Of MediaContainers.Uniqueid)(New MediaContainers.Uniqueid() {nUniqueID})}
                                           })
                    Next
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Certifications
            If FilteredOptions.bMainCertifications Then
                If Result.ContentRatings IsNot Nothing AndAlso Result.ContentRatings.Results IsNot Nothing AndAlso Result.ContentRatings.Results.Count > 0 Then
                    For Each aCountry In Result.ContentRatings.Results
                        If Not String.IsNullOrEmpty(aCountry.Rating) Then
                            Dim CertificationLanguage = APIXML.CertificationLanguages.Language.FirstOrDefault(Function(l) l.abbreviation = aCountry.Iso_3166_1.ToLower)
                            If CertificationLanguage IsNot Nothing AndAlso CertificationLanguage.name IsNot Nothing AndAlso Not String.IsNullOrEmpty(CertificationLanguage.name) Then
                                nTVShow.Certifications.Add(String.Concat(CertificationLanguage.name, ":", aCountry.Rating))
                            Else
                                _Logger.Warn("Unhandled certification language encountered: {0}", aCountry.Iso_3166_1.ToLower)
                            End If
                        End If
                    Next
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Countries 'TODO: Change from OriginCountry to ProductionCountries (not yet supported by API)
            'If FilteredOptions.bMainCountry Then
            '    If Show.OriginCountry IsNot Nothing AndAlso Show.OriginCountry.Count > 0 Then
            '        For Each aCountry As String In Show.OriginCountry
            '            nShow.Countries.Add(aCountry)
            '        Next
            '    End If
            'End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Creators
            If FilteredOptions.bMainCreators Then
                If Result.CreatedBy IsNot Nothing Then
                    For Each aCreator As TMDbLib.Objects.TvShows.CreatedBy In Result.CreatedBy
                        nTVShow.Creators.Add(aCreator.Name)
                    Next
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Genres
            If FilteredOptions.bMainGenres Then
                Dim aGenres As List(Of TMDbLib.Objects.General.Genre) = Nothing
                If Result.Genres Is Nothing OrElse (Result.Genres IsNot Nothing AndAlso Result.Genres.Count = 0) Then
                    If _AddonSettings.FallBackEng AndAlso ResultE.Genres IsNot Nothing AndAlso ResultE.Genres.Count > 0 Then
                        aGenres = ResultE.Genres
                    End If
                Else
                    aGenres = Result.Genres
                End If

                If aGenres IsNot Nothing Then
                    For Each tGenre As TMDbLib.Objects.General.Genre In aGenres
                        nTVShow.Genres.Add(tGenre.Name)
                    Next
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'OriginalTitle
            If FilteredOptions.bMainOriginalTitle Then
                If Result.OriginalName Is Nothing OrElse (Result.OriginalName IsNot Nothing AndAlso String.IsNullOrEmpty(Result.OriginalName)) Then
                    If _AddonSettings.FallBackEng AndAlso ResultE.OriginalName IsNot Nothing AndAlso Not String.IsNullOrEmpty(ResultE.OriginalName) Then
                        nTVShow.OriginalTitle = ResultE.OriginalName
                    End If
                Else
                    nTVShow.OriginalTitle = ResultE.OriginalName
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Plot
            If FilteredOptions.bMainPlot Then
                If Result.Overview Is Nothing OrElse (Result.Overview IsNot Nothing AndAlso String.IsNullOrEmpty(Result.Overview)) Then
                    If _AddonSettings.FallBackEng AndAlso ResultE.Overview IsNot Nothing AndAlso Not String.IsNullOrEmpty(ResultE.Overview) Then
                        nTVShow.Plot = ResultE.Overview
                    End If
                Else
                    nTVShow.Plot = Result.Overview
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Posters (only for SearchResult dialog, auto fallback to "en" by TMDB)
            If GetPoster Then
                If Result.PosterPath IsNot Nothing AndAlso Not String.IsNullOrEmpty(Result.PosterPath) Then
                    _Poster = String.Concat(_Client.Config.Images.BaseUrl, "w92", Result.PosterPath)
                Else
                    _Poster = String.Empty
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Premiered
            If FilteredOptions.bMainPremiered Then
                Dim ScrapedDate As String = String.Empty
                If Result.FirstAirDate Is Nothing OrElse (Result.FirstAirDate IsNot Nothing AndAlso String.IsNullOrEmpty(CStr(Result.FirstAirDate))) Then
                    If _AddonSettings.FallBackEng AndAlso ResultE.FirstAirDate IsNot Nothing AndAlso Not String.IsNullOrEmpty(CStr(ResultE.FirstAirDate)) Then
                        ScrapedDate = CStr(ResultE.FirstAirDate)
                    End If
                Else
                    ScrapedDate = CStr(Result.FirstAirDate)
                End If
                If Not String.IsNullOrEmpty(ScrapedDate) Then
                    Dim RelDate As Date
                    If Date.TryParse(ScrapedDate, RelDate) Then
                        'always save date in same date format not depending on users language setting!
                        nTVShow.Premiered = RelDate.ToString("yyyy-MM-dd")
                    Else
                        nTVShow.Premiered = ScrapedDate
                    End If
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Rating
            If FilteredOptions.bMainRating Then
                nTVShow.Ratings.Add(New MediaContainers.RatingDetails With {.Max = 10, .Name = "themoviedb", .Value = Result.VoteAverage, .Votes = Result.VoteCount})
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Runtime
            If FilteredOptions.bMainRuntime Then
                If Result.EpisodeRunTime Is Nothing OrElse Result.EpisodeRunTime.Count = 0 Then
                    If _AddonSettings.FallBackEng AndAlso ResultE.EpisodeRunTime IsNot Nothing AndAlso ResultE.EpisodeRunTime.Count > 0 Then
                        nTVShow.Runtime = CStr(ResultE.EpisodeRunTime.Item(0))
                    End If
                Else
                    nTVShow.Runtime = CStr(Result.EpisodeRunTime.Item(0))
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Status
            If FilteredOptions.bMainStatus Then
                If Result.Status Is Nothing OrElse (Result.Status IsNot Nothing AndAlso String.IsNullOrEmpty(Result.Status)) Then
                    If _AddonSettings.FallBackEng AndAlso ResultE.Status IsNot Nothing AndAlso Not String.IsNullOrEmpty(ResultE.Status) Then
                        nTVShow.Status = ResultE.Status
                    End If
                Else
                    nTVShow.Status = Result.Status
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Studios
            If FilteredOptions.bMainStudios Then
                If Result.Networks IsNot Nothing AndAlso Result.Networks.Count > 0 Then
                    For Each aStudio In Result.Networks
                        nTVShow.Studios.Add(aStudio.Name)
                    Next
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Title
            If FilteredOptions.bMainTitle Then
                If Result.Name Is Nothing OrElse (Result.Name IsNot Nothing AndAlso String.IsNullOrEmpty(Result.Name)) Then
                    If _AddonSettings.FallBackEng AndAlso ResultE.Name IsNot Nothing AndAlso Not String.IsNullOrEmpty(ResultE.Name) Then
                        nTVShow.Title = ResultE.Name
                    End If
                Else
                    nTVShow.Title = Result.Name
                End If
            End If

            If bwTMDB.CancellationPending Then Return Nothing

            'Seasons and Episodes
            If ScrapeModifiers.withEpisodes OrElse ScrapeModifiers.withSeasons Then
                For Each aSeason As TMDbLib.Objects.Search.SearchTvSeason In Result.Seasons
                    GetInfo_TVSeason(nTVShow, Result.Id, aSeason.SeasonNumber, ScrapeModifiers, FilteredOptions)
                Next
            End If
        Else
            Return Nothing
        End If

        Return nTVShow
    End Function

    Public Function GetInfo_TVEpisode(ByVal ShowID As Integer, ByVal Aired As String, ByRef FilteredOptions As Structures.ScrapeOptions) As MediaContainers.EpisodeDetails
        Dim nTVEpisode As New MediaContainers.EpisodeDetails
        Dim ShowInfo As TMDbLib.Objects.TvShows.TvShow

        Dim showAPIResult As Task(Of TMDbLib.Objects.TvShows.TvShow)
        showAPIResult = Task.Run(Function() _Client.GetTvShowAsync(ShowID))

        ShowInfo = showAPIResult.Result

        For Each aSeason As TMDbLib.Objects.Search.SearchTvSeason In ShowInfo.Seasons
            Dim seasonAPIResult As Task(Of TMDbLib.Objects.TvShows.TvSeason)
            seasonAPIResult = Task.Run(Function() _Client.GetTvSeasonAsync(ShowID, aSeason.SeasonNumber, TMDbLib.Objects.TvShows.TvSeasonMethods.Credits Or TMDbLib.Objects.TvShows.TvSeasonMethods.ExternalIds))

            Dim SeasonInfo As TMDbLib.Objects.TvShows.TvSeason = seasonAPIResult.Result
            Dim EpisodeList As IEnumerable(Of TMDbLib.Objects.Search.TvSeasonEpisode) = SeasonInfo.Episodes.Where(Function(f) CBool(f.AirDate = CDate(Aired)))
            If EpisodeList IsNot Nothing AndAlso EpisodeList.Count = 1 Then
                Return GetInfo_TVEpisode(ShowID, EpisodeList(0).SeasonNumber, EpisodeList(0).EpisodeNumber, FilteredOptions)
            ElseIf EpisodeList.Count > 0 Then
                Return Nothing
            End If
        Next

        Return Nothing
    End Function

    Public Function GetInfo_TVEpisode(ByVal tmdbID As Integer, ByVal SeasonNumber As Integer, ByVal EpisodeNumber As Integer, ByRef FilteredOptions As Structures.ScrapeOptions) As MediaContainers.EpisodeDetails
        Dim APIResult As Task(Of TMDbLib.Objects.TvShows.TvEpisode)
        APIResult = Task.Run(Function() _Client.GetTvEpisodeAsync(tmdbID, SeasonNumber, EpisodeNumber, TMDbLib.Objects.TvShows.TvEpisodeMethods.Credits Or TMDbLib.Objects.TvShows.TvEpisodeMethods.ExternalIds))

        If APIResult IsNot Nothing AndAlso APIResult.Exception Is Nothing AndAlso APIResult.Result IsNot Nothing Then
            Dim EpisodeInfo As TMDbLib.Objects.TvShows.TvEpisode = APIResult.Result

            If EpisodeInfo Is Nothing OrElse EpisodeInfo.Id Is Nothing OrElse Not EpisodeInfo.Id > 0 Then
                _Logger.Error(String.Format("Can't scrape or episode not found: tmdbID={0}, Season{1}, Episode{2}", tmdbID, SeasonNumber, EpisodeNumber))
                Return Nothing
            End If

            Dim nEpisode As MediaContainers.EpisodeDetails = GetInfo_TVEpisode(EpisodeInfo, FilteredOptions)
            Return nEpisode
        Else
            _Logger.Error(String.Format("Can't scrape or episode not found: tmdbID={0}, Season{1}, Episode{2}", tmdbID, SeasonNumber, EpisodeNumber))
            Return Nothing
        End If
    End Function

    Public Function GetInfo_TVEpisode(ByRef EpisodeInfo As TMDbLib.Objects.TvShows.TvEpisode, ByRef FilteredOptions As Structures.ScrapeOptions) As MediaContainers.EpisodeDetails
        Dim nTVEpisode As New MediaContainers.EpisodeDetails

        nTVEpisode.Scrapersource = "TMDB"

        'IDs
        nTVEpisode.UniqueIDs.TMDbId = CStr(EpisodeInfo.Id)
        If EpisodeInfo.ExternalIds IsNot Nothing AndAlso EpisodeInfo.ExternalIds.TvdbId IsNot Nothing Then nTVEpisode.UniqueIDs.TVDbId = EpisodeInfo.ExternalIds.TvdbId
        If EpisodeInfo.ExternalIds IsNot Nothing AndAlso EpisodeInfo.ExternalIds.ImdbId IsNot Nothing Then nTVEpisode.UniqueIDs.IMDbId = EpisodeInfo.ExternalIds.ImdbId

        'Episode # Standard
        If EpisodeInfo.EpisodeNumber >= 0 Then
            nTVEpisode.Episode = EpisodeInfo.EpisodeNumber
        End If

        'Season # Standard
        If EpisodeInfo.SeasonNumber >= 0 Then
            nTVEpisode.Season = CInt(EpisodeInfo.SeasonNumber)
        End If

        'Cast (Actors)
        If FilteredOptions.bEpisodeActors Then
            If EpisodeInfo.Credits IsNot Nothing AndAlso EpisodeInfo.Credits.Cast IsNot Nothing Then
                For Each aCast As TMDbLib.Objects.TvShows.Cast In EpisodeInfo.Credits.Cast
                    Dim nUniqueID As New MediaContainers.Uniqueid With {
                        .Type = "tmdb",
                        .Value = aCast.Id.ToString}
                    nTVEpisode.Actors.Add(New MediaContainers.Person With {
                                          .Name = aCast.Name,
                                          .Role = aCast.Character,
                                          .URLOriginal = If(Not String.IsNullOrEmpty(aCast.ProfilePath), String.Concat(_Client.Config.Images.BaseUrl, "original", aCast.ProfilePath), String.Empty),
                                          .UniqueIDs = New MediaContainers.UniqueidContainer With {.Items = New List(Of MediaContainers.Uniqueid)(New MediaContainers.Uniqueid() {nUniqueID})}
                                          })
                Next
            End If
        End If

        'Aired
        If FilteredOptions.bEpisodeAired Then
            If EpisodeInfo.AirDate IsNot Nothing Then
                Dim ScrapedDate As String = CStr(EpisodeInfo.AirDate)
                If Not String.IsNullOrEmpty(ScrapedDate) AndAlso Not ScrapedDate = "00:00:00" Then
                    Dim RelDate As Date
                    If Date.TryParse(ScrapedDate, RelDate) Then
                        'always save date in same date format not depending on users language setting!
                        nTVEpisode.Aired = RelDate.ToString("yyyy-MM-dd")
                    Else
                        nTVEpisode.Aired = ScrapedDate
                    End If
                End If
            End If
        End If

        'Director / Writer
        If FilteredOptions.bEpisodeCredits OrElse FilteredOptions.bEpisodeDirectors Then
            If EpisodeInfo.Credits IsNot Nothing AndAlso EpisodeInfo.Credits.Crew IsNot Nothing Then
                For Each aCrew As TMDbLib.Objects.General.Crew In EpisodeInfo.Credits.Crew
                    If FilteredOptions.bEpisodeCredits AndAlso aCrew.Department = "Writing" AndAlso (aCrew.Job = "Author" OrElse aCrew.Job = "Screenplay" OrElse aCrew.Job = "Writer") Then
                        nTVEpisode.Credits.Add(aCrew.Name)
                    End If
                    If FilteredOptions.bEpisodeDirectors AndAlso aCrew.Department = "Directing" AndAlso aCrew.Job = "Director" Then
                        nTVEpisode.Directors.Add(aCrew.Name)
                    End If
                Next
            End If
        End If

        'Guest Stars
        If FilteredOptions.bEpisodeGuestStars Then
            If EpisodeInfo.GuestStars IsNot Nothing Then
                For Each aCast As TMDbLib.Objects.TvShows.Cast In EpisodeInfo.GuestStars
                    Dim nUniqueID As New MediaContainers.Uniqueid With {
                        .Type = "tmdb",
                        .Value = aCast.Id.ToString}
                    nTVEpisode.GuestStars.Add(New MediaContainers.Person With {
                                              .Name = aCast.Name,
                                              .Role = aCast.Character,
                                              .URLOriginal = If(Not String.IsNullOrEmpty(aCast.ProfilePath), String.Concat(_Client.Config.Images.BaseUrl, "original", aCast.ProfilePath), String.Empty),
                                              .UniqueIDs = New MediaContainers.UniqueidContainer With {.Items = New List(Of MediaContainers.Uniqueid)(New MediaContainers.Uniqueid() {nUniqueID})}
                                              })
                Next
            End If
        End If

        'OriginalTitle
        'TODO: implement
        'If FilteredOptions.bEpisodeOriginalTitle Then
        '    If EpisodeInfo.Name IsNot Nothing Then
        '        nTVEpisode.OriginalTitle = EpisodeInfo.Name
        '    End If
        'End If

        'Plot
        If FilteredOptions.bEpisodePlot Then
            If EpisodeInfo.Overview IsNot Nothing Then
                nTVEpisode.Plot = EpisodeInfo.Overview
            End If
        End If

        'Rating
        If FilteredOptions.bMainRating Then
            nTVEpisode.Ratings.Add(New MediaContainers.RatingDetails With {.Max = 10, .Name = "themoviedb", .Value = EpisodeInfo.VoteAverage, .Votes = EpisodeInfo.VoteCount})
        End If

        'ThumbPoster
        If EpisodeInfo.StillPath IsNot Nothing Then
            nTVEpisode.ThumbPoster.URLOriginal = _Client.Config.Images.BaseUrl & "original" & EpisodeInfo.StillPath
            nTVEpisode.ThumbPoster.URLThumb = _Client.Config.Images.BaseUrl & "w185" & EpisodeInfo.StillPath
        End If

        'Title
        If FilteredOptions.bEpisodeTitle Then
            If EpisodeInfo.Name IsNot Nothing Then
                nTVEpisode.Title = EpisodeInfo.Name
            End If
        End If

        Return nTVEpisode
    End Function

    Public Sub GetInfo_TVSeason(ByRef nTVShow As MediaContainers.TVShow, ByVal ShowID As Integer, ByVal SeasonNumber As Integer, ByRef ScrapeModifiers As Structures.ScrapeModifiers, ByRef FilteredOptions As Structures.ScrapeOptions)
        Dim nSeason As New MediaContainers.SeasonDetails

        Dim APIResult As Task(Of TMDbLib.Objects.TvShows.TvSeason)
        APIResult = Task.Run(Function() _Client.GetTvSeasonAsync(ShowID, SeasonNumber, TMDbLib.Objects.TvShows.TvSeasonMethods.Credits Or TMDbLib.Objects.TvShows.TvSeasonMethods.ExternalIds))

        If APIResult IsNot Nothing AndAlso APIResult.Exception Is Nothing AndAlso APIResult.Result IsNot Nothing Then
            Dim SeasonInfo As TMDbLib.Objects.TvShows.TvSeason = APIResult.Result

            nSeason.UniqueIDs.TMDbId = CStr(SeasonInfo.Id)
            If SeasonInfo.ExternalIds IsNot Nothing AndAlso SeasonInfo.ExternalIds.TvdbId IsNot Nothing Then nSeason.UniqueIDs.TVDbId = SeasonInfo.ExternalIds.TvdbId

            If ScrapeModifiers.withSeasons Then

                'Aired
                If FilteredOptions.bSeasonAired AndAlso SeasonInfo.AirDate IsNot Nothing Then
                    Dim ScrapedDate As String = CStr(SeasonInfo.AirDate)
                    If Not String.IsNullOrEmpty(ScrapedDate) Then
                        Dim RelDate As Date
                        If Date.TryParse(ScrapedDate, RelDate) Then
                            'always save date in same date format not depending on users language setting!
                            nSeason.Aired = RelDate.ToString("yyyy-MM-dd")
                        Else
                            nSeason.Aired = ScrapedDate
                        End If
                    End If
                End If

                'Plot
                If FilteredOptions.bSeasonPlot AndAlso SeasonInfo.Overview IsNot Nothing Then
                    nSeason.Plot = SeasonInfo.Overview
                End If

                'Season #
                If SeasonInfo.SeasonNumber >= 0 Then
                    nSeason.Season = SeasonInfo.SeasonNumber
                End If

                'Title
                If FilteredOptions.bSeasonTitle AndAlso SeasonInfo.Name IsNot Nothing Then
                    nSeason.Title = SeasonInfo.Name
                End If

                nTVShow.KnownSeasons.Add(nSeason)
            End If

            If ScrapeModifiers.withEpisodes AndAlso SeasonInfo.Episodes IsNot Nothing Then
                For Each aEpisode As TMDbLib.Objects.Search.TvSeasonEpisode In SeasonInfo.Episodes
                    Dim nEpisode = GetInfo_TVEpisode(ShowID, aEpisode.SeasonNumber, aEpisode.EpisodeNumber, FilteredOptions)
                    If nEpisode IsNot Nothing Then nTVShow.KnownEpisodes.Add(nEpisode)
                Next
            End If
        Else
            _Logger.Error(String.Format("Can't scrape or season not found: ShowID={0}, Season={1}", ShowID, SeasonNumber))
        End If
    End Sub

    Public Function GetInfo_TVSeason(ByVal tmdbID As Integer, ByVal SeasonNumber As Integer, ByRef FilteredOptions As Structures.ScrapeOptions) As MediaContainers.SeasonDetails
        Dim APIResult As Task(Of TMDbLib.Objects.TvShows.TvSeason)
        APIResult = Task.Run(Function() _Client.GetTvSeasonAsync(tmdbID, SeasonNumber, TMDbLib.Objects.TvShows.TvSeasonMethods.Credits Or TMDbLib.Objects.TvShows.TvSeasonMethods.ExternalIds))

        If APIResult IsNot Nothing AndAlso APIResult.Exception Is Nothing AndAlso APIResult.Result IsNot Nothing Then
            Dim SeasonInfo As TMDbLib.Objects.TvShows.TvSeason = APIResult.Result

            If SeasonInfo Is Nothing OrElse SeasonInfo.Id Is Nothing OrElse Not SeasonInfo.Id > 0 Then
                _Logger.Error(String.Format("Can't scrape or season not found: tmdbID={0}, Season={1}", tmdbID, SeasonNumber))
                Return Nothing
            End If

            Dim nTVSeason As MediaContainers.SeasonDetails = GetInfo_TVSeason(SeasonInfo, FilteredOptions)
            Return nTVSeason
        Else
            _Logger.Error(String.Format("Can't scrape or season not found: tmdbID={0}, Season={1}", tmdbID, SeasonNumber))
            Return Nothing
        End If
    End Function

    Public Function GetInfo_TVSeason(ByRef SeasonInfo As TMDbLib.Objects.TvShows.TvSeason, ByRef FilteredOptions As Structures.ScrapeOptions) As MediaContainers.SeasonDetails
        Dim nTVSeason As New MediaContainers.SeasonDetails

        nTVSeason.Scrapersource = "TMDB"

        'IDs
        nTVSeason.UniqueIDs.TMDbId = CStr(SeasonInfo.Id)
        If SeasonInfo.ExternalIds IsNot Nothing AndAlso SeasonInfo.ExternalIds.TvdbId IsNot Nothing Then nTVSeason.UniqueIDs.TVDbId = SeasonInfo.ExternalIds.TvdbId

        'Season #
        If SeasonInfo.SeasonNumber >= 0 Then
            nTVSeason.Season = SeasonInfo.SeasonNumber
        End If

        'Aired
        If FilteredOptions.bSeasonAired AndAlso SeasonInfo.AirDate IsNot Nothing Then
            Dim ScrapedDate As String = CStr(SeasonInfo.AirDate)
            If Not String.IsNullOrEmpty(ScrapedDate) Then
                Dim RelDate As Date
                If Date.TryParse(ScrapedDate, RelDate) Then
                    'always save date in same date format not depending on users language setting!
                    nTVSeason.Aired = RelDate.ToString("yyyy-MM-dd")
                Else
                    nTVSeason.Aired = ScrapedDate
                End If
            End If
        End If

        'Plot
        If FilteredOptions.bSeasonPlot AndAlso SeasonInfo.Overview IsNot Nothing Then
            nTVSeason.Plot = SeasonInfo.Overview
        End If

        'Title
        If FilteredOptions.bSeasonTitle AndAlso SeasonInfo.Name IsNot Nothing Then
            nTVSeason.Title = SeasonInfo.Name
        End If

        Return nTVSeason
    End Function

    Public Function GetTMDBbyIMDB(ByVal imdbID As String) As String
        Dim tmdbID As String = String.Empty

        Try
            Dim APIResult As Task(Of TMDbLib.Objects.Find.FindContainer)
            APIResult = Task.Run(Function() _Client.FindAsync(TMDbLib.Objects.Find.FindExternalSource.Imdb, imdbID))

            If APIResult IsNot Nothing AndAlso APIResult.Exception Is Nothing AndAlso APIResult.Result IsNot Nothing AndAlso
                    APIResult.Result.TvResults IsNot Nothing AndAlso APIResult.Result.TvResults.Count > 0 Then
                tmdbID = APIResult.Result.TvResults.Item(0).Id.ToString
            End If

        Catch ex As Exception
            _Logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try

        Return tmdbID
    End Function

    Public Function GetTMDBbyTVDB(ByVal tvdbID As String) As String
        Dim tmdbID As String = String.Empty

        Try
            Dim APIResult As Task(Of TMDbLib.Objects.Find.FindContainer)
            APIResult = Task.Run(Function() _Client.FindAsync(TMDbLib.Objects.Find.FindExternalSource.TvDb, tvdbID))

            If APIResult IsNot Nothing AndAlso APIResult.Exception Is Nothing AndAlso APIResult.Result IsNot Nothing AndAlso
                    APIResult.Result.TvResults IsNot Nothing AndAlso APIResult.Result.TvResults.Count > 0 Then
                tmdbID = APIResult.Result.TvResults.Item(0).Id.ToString
            End If

        Catch ex As Exception
            _Logger.Error(ex, New StackFrame().GetMethod().Name)
        End Try

        Return tmdbID
    End Function

    Public Function GetMovieStudios(ByVal strID As String) As List(Of String)
        If String.IsNullOrEmpty(strID) Then Return New List(Of String)

        Dim alStudio As New List(Of String)
        Dim Movie As TMDbLib.Objects.Movies.Movie = Nothing

        Dim APIResult As Task(Of TMDbLib.Objects.Movies.Movie) = Nothing

        If strID.ToLower.StartsWith("tt") Then
            APIResult = Task.Run(Function() _Client.GetMovieAsync(strID))
        ElseIf Integer.TryParse(strID, 0) Then
            APIResult = Task.Run(Function() _Client.GetMovieAsync(CInt(strID)))
        End If

        If APIResult IsNot Nothing AndAlso APIResult.Result IsNot Nothing Then
            Movie = APIResult.Result
        End If

        If Movie IsNot Nothing AndAlso Movie.ProductionCompanies IsNot Nothing AndAlso Movie.ProductionCompanies.Count > 0 Then
            For Each cStudio In Movie.ProductionCompanies
                alStudio.Add(cStudio.Name)
            Next
        End If

        Return alStudio
    End Function

    Public Function GetSearchMovieInfo(ByVal strMovieName As String, ByRef oDBMovie As Database.DBElement, ByVal eType As Enums.ScrapeType, ByVal FilteredOptions As Structures.ScrapeOptions) As MediaContainers.Movie
        Dim r As SearchResults_Movie = SearchMovie(strMovieName, oDBMovie.Movie.Year)

        Select Case eType
            Case Enums.ScrapeType.AllAsk, Enums.ScrapeType.FilterAsk, Enums.ScrapeType.MarkedAsk, Enums.ScrapeType.MissingAsk, Enums.ScrapeType.NewAsk, Enums.ScrapeType.SelectedAsk, Enums.ScrapeType.SingleField
                If r.Matches.Count = 1 Then
                    Return GetInfo_Movie(r.Matches.Item(0).UniqueIDs.TMDbId, FilteredOptions, False)
                Else
                    Using dlgSearch As New dlgSearchResults_Movie(_AddonSettings, Me)
                        If dlgSearch.ShowDialog(r, strMovieName, oDBMovie.FileItem.FirstPathFromStack) = DialogResult.OK Then
                            If dlgSearch.Result.UniqueIDs.IMDbIdSpecified Then
                                Return GetInfo_Movie(dlgSearch.Result.UniqueIDs.TMDbId, FilteredOptions, False)
                            End If
                        End If
                    End Using
                End If

            Case Enums.ScrapeType.AllSkip, Enums.ScrapeType.FilterSkip, Enums.ScrapeType.MarkedSkip, Enums.ScrapeType.MissingSkip, Enums.ScrapeType.NewSkip, Enums.ScrapeType.SelectedSkip
                If r.Matches.Count = 1 Then
                    Return GetInfo_Movie(r.Matches.Item(0).UniqueIDs.TMDbId, FilteredOptions, False)
                End If

            Case Enums.ScrapeType.AllAuto, Enums.ScrapeType.FilterAuto, Enums.ScrapeType.MarkedAuto, Enums.ScrapeType.MissingAuto, Enums.ScrapeType.NewAuto, Enums.ScrapeType.SelectedAuto, Enums.ScrapeType.SingleScrape
                If r.Matches.Count > 0 Then
                    Return GetInfo_Movie(r.Matches.Item(0).UniqueIDs.TMDbId, FilteredOptions, False)
                End If
        End Select

        Return Nothing
    End Function

    Public Function GetSearchMovieSetInfo(ByVal strMovieSetName As String, ByRef oDBMovieSet As Database.DBElement, ByVal eType As Enums.ScrapeType, ByVal FilteredOptions As Structures.ScrapeOptions) As MediaContainers.MovieSet
        Dim r As SearchResults_MovieSet = SearchMovieSet(strMovieSetName)

        Select Case eType
            Case Enums.ScrapeType.AllAsk, Enums.ScrapeType.FilterAsk, Enums.ScrapeType.MarkedAsk, Enums.ScrapeType.MissingAsk, Enums.ScrapeType.NewAsk, Enums.ScrapeType.SelectedAsk, Enums.ScrapeType.SingleField
                If r.Matches.Count = 1 Then
                    Return GetInfo_MovieSet(r.Matches.Item(0).UniqueIDs.TMDbId, FilteredOptions, False)
                Else
                    Using dlgSearch As New dlgSearchResults_MovieSet(_AddonSettings, Me)
                        If dlgSearch.ShowDialog(r, strMovieSetName) = DialogResult.OK Then
                            If Not String.IsNullOrEmpty(dlgSearch.Result.UniqueIDs.TMDbId) Then
                                Return GetInfo_MovieSet(dlgSearch.Result.UniqueIDs.TMDbId, FilteredOptions, False)
                            End If
                        End If
                    End Using
                End If

            Case Enums.ScrapeType.AllSkip, Enums.ScrapeType.FilterSkip, Enums.ScrapeType.MarkedSkip, Enums.ScrapeType.MissingSkip, Enums.ScrapeType.NewSkip, Enums.ScrapeType.SelectedSkip
                If r.Matches.Count = 1 Then
                    Return GetInfo_MovieSet(r.Matches.Item(0).UniqueIDs.TMDbId, FilteredOptions, False)
                End If

            Case Enums.ScrapeType.AllAuto, Enums.ScrapeType.FilterAuto, Enums.ScrapeType.MarkedAuto, Enums.ScrapeType.MissingAuto, Enums.ScrapeType.NewAuto, Enums.ScrapeType.SelectedAuto, Enums.ScrapeType.SingleScrape
                If r.Matches.Count > 0 Then
                    Return GetInfo_MovieSet(r.Matches.Item(0).UniqueIDs.TMDbId, FilteredOptions, False)
                End If
        End Select

        Return Nothing
    End Function

    Public Function GetSearchTVShowInfo(ByVal strShowName As String, ByRef oDBTV As Database.DBElement, ByVal eType As Enums.ScrapeType, ByRef ScrapeModifiers As Structures.ScrapeModifiers, ByRef FilteredOptions As Structures.ScrapeOptions) As MediaContainers.TVShow
        Dim r As SearchResults_TVShow = SearchTVShow(strShowName)

        Select Case eType
            Case Enums.ScrapeType.AllAsk, Enums.ScrapeType.FilterAsk, Enums.ScrapeType.MarkedAsk, Enums.ScrapeType.MissingAsk, Enums.ScrapeType.NewAsk, Enums.ScrapeType.SelectedAsk, Enums.ScrapeType.SingleField
                If r.Matches.Count = 1 Then
                    Return GetInfo_TVShow(r.Matches.Item(0).UniqueIDs.TMDbId, ScrapeModifiers, FilteredOptions, False)
                Else
                    Using dlgSearch As New dlgSearchResults_TV(_AddonSettings, Me)
                        If dlgSearch.ShowDialog(r, strShowName, oDBTV.ShowPath) = DialogResult.OK Then
                            If Not String.IsNullOrEmpty(dlgSearch.Result.UniqueIDs.TMDbId) Then
                                Return GetInfo_TVShow(dlgSearch.Result.UniqueIDs.TMDbId, ScrapeModifiers, FilteredOptions, False)
                            End If
                        End If
                    End Using
                End If

            Case Enums.ScrapeType.AllSkip, Enums.ScrapeType.FilterSkip, Enums.ScrapeType.MarkedSkip, Enums.ScrapeType.MissingSkip, Enums.ScrapeType.NewSkip, Enums.ScrapeType.SelectedSkip
                If r.Matches.Count = 1 Then
                    Return GetInfo_TVShow(r.Matches.Item(0).UniqueIDs.TMDbId, ScrapeModifiers, FilteredOptions, False)
                End If

            Case Enums.ScrapeType.AllAuto, Enums.ScrapeType.FilterAuto, Enums.ScrapeType.MarkedAuto, Enums.ScrapeType.MissingAuto, Enums.ScrapeType.NewAuto, Enums.ScrapeType.SelectedAuto, Enums.ScrapeType.SingleScrape
                If r.Matches.Count > 0 Then
                    Return GetInfo_TVShow(r.Matches.Item(0).UniqueIDs.TMDbId, ScrapeModifiers, FilteredOptions, False)
                End If
        End Select

        Return Nothing
    End Function

    Public Sub GetSearchMovieInfoAsync(ByVal tmdbID As String, ByRef FilteredOptions As Structures.ScrapeOptions)
        '' The rule is that if there is a tt is an IMDB otherwise is a TMDB
        If Not bwTMDB.IsBusy Then
            bwTMDB.WorkerReportsProgress = False
            bwTMDB.WorkerSupportsCancellation = True
            bwTMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.SearchDetails_Movie,
                  .Parameter = tmdbID, .ScrapeOptions = FilteredOptions})
        End If
    End Sub

    Public Sub GetSearchMovieSetInfoAsync(ByVal tmdbColID As String, ByRef FilteredOptions As Structures.ScrapeOptions)
        '' The rule is that if there is a tt is an IMDB otherwise is a TMDB
        If Not bwTMDB.IsBusy Then
            bwTMDB.WorkerReportsProgress = False
            bwTMDB.WorkerSupportsCancellation = True
            bwTMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.SearchDetails_MovieSet,
                  .Parameter = tmdbColID, .ScrapeOptions = FilteredOptions})
        End If
    End Sub

    Public Sub GetSearchTVShowInfoAsync(ByVal tmdbID As String, ByRef FilteredOptions As Structures.ScrapeOptions)
        '' The rule is that if there is a tt is an IMDB otherwise is a TMDB
        If Not bwTMDB.IsBusy Then
            bwTMDB.WorkerReportsProgress = False
            bwTMDB.WorkerSupportsCancellation = True
            bwTMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.SearchDetails_TVShow,
                  .Parameter = tmdbID, .ScrapeOptions = FilteredOptions})
        End If
    End Sub

    Public Function GetTrailers(ByVal TMDbID As String) As List(Of MediaContainers.Trailer)
        Dim alTrailers As New List(Of MediaContainers.Trailer)
        Dim trailers As TMDbLib.Objects.General.ResultContainer(Of TMDbLib.Objects.General.Video)

        If String.IsNullOrEmpty(TMDbID) OrElse Not Integer.TryParse(TMDbID, 0) Then Return alTrailers

        Dim APIResult As Task(Of TMDbLib.Objects.Movies.Movie)
        APIResult = Task.Run(Function() _Client.GetMovieAsync(CInt(TMDbID), TMDbLib.Objects.Movies.MovieMethods.Videos))

        trailers = APIResult.Result.Videos
        If trailers Is Nothing OrElse trailers.Results Is Nothing OrElse trailers.Results.Count = 0 AndAlso _AddonSettings.FallBackEng Then
            APIResult = Task.Run(Function() _ClientEN.GetMovieAsync(CInt(TMDbID), TMDbLib.Objects.Movies.MovieMethods.Videos))
            trailers = APIResult.Result.Videos
            If trailers Is Nothing OrElse trailers.Results Is Nothing OrElse trailers.Results.Count = 0 Then
                Return alTrailers
            End If
        End If
        If trailers IsNot Nothing AndAlso trailers.Results IsNot Nothing Then
            For Each Video As TMDbLib.Objects.General.Video In trailers.Results.Where(Function(f) f.Site = "YouTube")
                Dim tLink As String = String.Format("http://www.youtube.com/watch?v={0}", Video.Key)
                If YouTube.Scraper.IsAvailable(tLink) Then
                    Dim tName As String = YouTube.Scraper.GetVideoTitle(tLink)
                    alTrailers.Add(New MediaContainers.Trailer With {
                                           .LongLang = If(String.IsNullOrEmpty(Video.Iso_639_1), String.Empty, Localization.ISOGetLangByCode2(Video.Iso_639_1)),
                                           .Quality = GetVideoQuality(Video.Size),
                                           .Scraper = "TMDB",
                                           .ShortLang = If(String.IsNullOrEmpty(Video.Iso_639_1), String.Empty, Video.Iso_639_1),
                                           .Source = Video.Site,
                                           .Title = tName,
                                           .Type = GetVideoType(Video.Type),
                                           .URLWebsite = tLink})
                End If
            Next
        End If

        Return alTrailers
    End Function

    Private Function GetVideoQuality(ByRef Size As Integer) As Enums.TrailerVideoQuality
        Select Case Size
            Case 1080
                Return Enums.TrailerVideoQuality.HD1080p
            Case 720
                Return Enums.TrailerVideoQuality.HD720p
            Case 480
                Return Enums.TrailerVideoQuality.HQ480p
            Case Else
                Return Enums.TrailerVideoQuality.Any
        End Select
    End Function

    Private Function GetVideoType(ByRef Type As String) As Enums.TrailerType
        Select Case Type.ToLower
            Case "clip"
                Return Enums.TrailerType.Clip
            Case "featurette"
                Return Enums.TrailerType.Featurette
            Case "teaser"
                Return Enums.TrailerType.Teaser
            Case "trailer"
                Return Enums.TrailerType.Trailer
            Case Else
                Return Enums.TrailerType.Any
        End Select
    End Function

    Public Sub SearchAsync_Movie(ByVal title As String, ByRef filterOptions As Structures.ScrapeOptions, Optional ByVal year As Integer = 0)
        If Not bwTMDB.IsBusy Then
            bwTMDB.WorkerReportsProgress = False
            bwTMDB.WorkerSupportsCancellation = True
            bwTMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.Movies,
                  .Parameter = title, .ScrapeOptions = filterOptions, .Year = year})
        End If
    End Sub

    Public Sub SearchAsync_MovieSet(ByVal sMovieSet As String, ByRef filterOptions As Structures.ScrapeOptions)
        '' The rule is that if there is a tt is an IMDB otherwise is a TMDB
        If Not bwTMDB.IsBusy Then
            bwTMDB.WorkerReportsProgress = False
            bwTMDB.WorkerSupportsCancellation = True
            bwTMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.MovieSets,
                  .Parameter = sMovieSet, .ScrapeOptions = filterOptions})
        End If
    End Sub

    Public Sub SearchAsync_TVShow(ByVal title As String, ByRef filterOptions As Structures.ScrapeOptions)

        If Not bwTMDB.IsBusy Then
            bwTMDB.WorkerReportsProgress = False
            bwTMDB.WorkerSupportsCancellation = True
            bwTMDB.RunWorkerAsync(New Arguments With {.Search = SearchType.TVShows,
                  .Parameter = title, .ScrapeOptions = filterOptions})
        End If
    End Sub

    Private Function SearchMovie(ByVal title As String, ByVal year As Integer) As SearchResults_Movie
        If String.IsNullOrEmpty(title) Then Return New SearchResults_Movie

        Dim R As New SearchResults_Movie
        Dim iPage As Integer = 1
        Dim Movies As TMDbLib.Objects.General.SearchContainer(Of TMDbLib.Objects.Search.SearchMovie)
        Dim iPageTotal As Integer
        Dim bFallbackToEng As Boolean

        Dim APIResult As Task(Of TMDbLib.Objects.General.SearchContainer(Of TMDbLib.Objects.Search.SearchMovie))
        APIResult = Task.Run(Function() _Client.SearchMovieAsync(title, iPage, _AddonSettings.GetAdultItems, year))

        Movies = APIResult.Result

        If Movies.TotalResults = 0 AndAlso _AddonSettings.FallBackEng Then
            APIResult = Task.Run(Function() _ClientEN.SearchMovieAsync(title, iPage, _AddonSettings.GetAdultItems, year))
            Movies = APIResult.Result
            bFallbackToEng = True
        End If

        'try -1 year if no search result was found
        If Movies.TotalResults = 0 AndAlso year > 0 AndAlso _AddonSettings.SearchDeviant Then
            APIResult = Task.Run(Function() _ClientEN.SearchMovieAsync(title, iPage, _AddonSettings.GetAdultItems, year - 1))
            Movies = APIResult.Result

            If Movies.TotalResults = 0 AndAlso _AddonSettings.FallBackEng Then
                APIResult = Task.Run(Function() _ClientEN.SearchMovieAsync(title, iPage, _AddonSettings.GetAdultItems, year - 1))
                Movies = APIResult.Result
                bFallbackToEng = True
            End If

            'still no search result, try +1 year
            If Movies.TotalResults = 0 Then
                APIResult = Task.Run(Function() _ClientEN.SearchMovieAsync(title, iPage, _AddonSettings.GetAdultItems, year + 1))
                Movies = APIResult.Result

                If Movies.TotalResults = 0 AndAlso _AddonSettings.FallBackEng Then
                    APIResult = Task.Run(Function() _ClientEN.SearchMovieAsync(title, iPage, _AddonSettings.GetAdultItems, year + 1))
                    Movies = APIResult.Result
                    bFallbackToEng = True
                End If
            End If
        End If

        If Movies.TotalResults > 0 Then
            iPageTotal = Movies.TotalPages
            While iPage <= iPageTotal AndAlso iPage <= 3
                If Movies.Results IsNot Nothing Then
                    For Each aMovie In Movies.Results
                        Dim tOriginalTitle As String = String.Empty
                        Dim tPlot As String = String.Empty
                        Dim tThumbPoster As MediaContainers.Image = New MediaContainers.Image
                        Dim tTitle As String = String.Empty
                        Dim tYear As Integer

                        If aMovie.OriginalTitle IsNot Nothing Then tOriginalTitle = aMovie.OriginalTitle
                        If aMovie.Overview IsNot Nothing Then tPlot = aMovie.Overview
                        If aMovie.PosterPath IsNot Nothing Then
                            tThumbPoster.URLOriginal = _Client.Config.Images.BaseUrl & "original" & aMovie.PosterPath
                            tThumbPoster.URLThumb = _Client.Config.Images.BaseUrl & "w185" & aMovie.PosterPath
                        End If
                        If aMovie.ReleaseDate IsNot Nothing AndAlso Not aMovie.ReleaseDate.HasValue Then tYear = aMovie.ReleaseDate.Value.Year
                        If aMovie.Title IsNot Nothing Then tTitle = aMovie.Title

                        Dim nMovie As MediaContainers.Movie = New MediaContainers.Movie With {
                            .OriginalTitle = tOriginalTitle,
                            .Plot = tPlot,
                            .Title = tTitle,
                            .ThumbPoster = tThumbPoster,
                            .Year = tYear}
                        nMovie.UniqueIDs.TMDbId = CStr(aMovie.Id)
                        R.Matches.Add(nMovie)
                    Next
                End If
                iPage = iPage + 1
                If bFallbackToEng Then
                    APIResult = Task.Run(Function() _ClientEN.SearchMovieAsync(title, iPage, _AddonSettings.GetAdultItems, year))
                    Movies = APIResult.Result
                Else
                    APIResult = Task.Run(Function() _Client.SearchMovieAsync(title, iPage, _AddonSettings.GetAdultItems, year))
                    Movies = APIResult.Result
                End If
            End While
        End If

        Return R
    End Function

    Private Function SearchMovieSet(ByVal strMovieSet As String) As SearchResults_MovieSet
        If String.IsNullOrEmpty(strMovieSet) Then Return New SearchResults_MovieSet

        Dim R As New SearchResults_MovieSet
        Dim Page As Integer = 1
        Dim MovieSets As TMDbLib.Objects.General.SearchContainer(Of TMDbLib.Objects.Search.SearchCollection)
        Dim TotP As Integer
        Dim aE As Boolean

        Dim APIResult As Task(Of TMDbLib.Objects.General.SearchContainer(Of TMDbLib.Objects.Search.SearchCollection))
        APIResult = Task.Run(Function() _Client.SearchCollectionAsync(strMovieSet, Page))

        MovieSets = APIResult.Result

        If MovieSets.TotalResults = 0 AndAlso _AddonSettings.FallBackEng Then
            APIResult = Task.Run(Function() _ClientEN.SearchCollectionAsync(strMovieSet, Page))
            MovieSets = APIResult.Result
            aE = True
        End If

        If MovieSets.TotalResults > 0 Then
            Dim t2 As String = String.Empty
            Dim t3 As String = String.Empty
            TotP = MovieSets.TotalPages
            While Page <= TotP AndAlso Page <= 3
                If MovieSets.Results IsNot Nothing Then
                    For Each aMovieSet In MovieSets.Results
                        If aMovieSet.Name IsNot Nothing AndAlso Not String.IsNullOrEmpty(aMovieSet.Name) Then
                            t2 = aMovieSet.Name
                        End If
                        'If aMovieSet.overview IsNot Nothing AndAlso Not String.IsNullOrEmpty(aMovieSet.overview) Then
                        '    t3 = aMovieSet.overview
                        'End If
                        Dim nMovieset = New MediaContainers.MovieSet With {
                            .Title = t2}
                        nMovieset.UniqueIDs.TMDbId = CStr(aMovieSet.Id)
                        R.Matches.Add(nMovieset)
                    Next
                End If
                Page = Page + 1
                If aE Then
                    APIResult = Task.Run(Function() _ClientEN.SearchCollectionAsync(strMovieSet, Page))
                    MovieSets = APIResult.Result
                Else
                    APIResult = Task.Run(Function() _Client.SearchCollectionAsync(strMovieSet, Page))
                    MovieSets = APIResult.Result
                End If
            End While
        End If

        Return R
    End Function

    Private Function SearchTVShow(ByVal strShow As String) As SearchResults_TVShow
        If String.IsNullOrEmpty(strShow) Then Return New SearchResults_TVShow

        Dim R As New SearchResults_TVShow
        Dim Page As Integer = 1
        Dim Shows As TMDbLib.Objects.General.SearchContainer(Of TMDbLib.Objects.Search.SearchTv)
        Dim TotP As Integer
        Dim aE As Boolean

        Dim APIResult As Task(Of TMDbLib.Objects.General.SearchContainer(Of TMDbLib.Objects.Search.SearchTv))
        APIResult = Task.Run(Function() _Client.SearchTvShowAsync(strShow, Page))

        Shows = APIResult.Result

        If Shows.TotalResults = 0 AndAlso _AddonSettings.FallBackEng Then
            APIResult = Task.Run(Function() _ClientEN.SearchTvShowAsync(strShow, Page))
            Shows = APIResult.Result
            aE = True
        End If

        If Shows.TotalResults > 0 Then
            Dim t1 As String = String.Empty
            Dim t2 As String = String.Empty
            TotP = Shows.TotalPages
            While Page <= TotP AndAlso Page <= 3
                If Shows.Results IsNot Nothing Then
                    For Each aShow In Shows.Results
                        If aShow.Name Is Nothing OrElse (aShow.Name IsNot Nothing AndAlso String.IsNullOrEmpty(aShow.Name)) Then
                            If aShow.OriginalName IsNot Nothing AndAlso Not String.IsNullOrEmpty(aShow.OriginalName) Then
                                t1 = aShow.OriginalName
                            End If
                        Else
                            t1 = aShow.Name
                        End If
                        If aShow.FirstAirDate IsNot Nothing AndAlso Not String.IsNullOrEmpty(CStr(aShow.FirstAirDate)) Then
                            t2 = CStr(aShow.FirstAirDate.Value.Year)
                        End If
                        Dim nTVShow As MediaContainers.TVShow = New MediaContainers.TVShow With {
                            .Premiered = t2,
                            .Title = t1}
                        nTVShow.UniqueIDs.TMDbId = CStr(aShow.Id)
                        R.Matches.Add(nTVShow)
                    Next
                End If
                Page = Page + 1
                If aE Then
                    APIResult = Task.Run(Function() _ClientEN.SearchTvShowAsync(strShow, Page))
                    Shows = APIResult.Result
                Else
                    APIResult = Task.Run(Function() _Client.SearchTvShowAsync(strShow, Page))
                    Shows = APIResult.Result
                End If
            End While
        End If

        Return R
    End Function

#End Region 'Methods

#Region "Nested Types"

    Private Structure Arguments

#Region "Fields"

        Dim FullCast As Boolean
        Dim FullCrew As Boolean
        Dim Parameter As String
        Dim ScrapeModifiers As Structures.ScrapeModifiers
        Dim ScrapeOptions As Structures.ScrapeOptions
        Dim Search As SearchType
        Dim Year As Integer

#End Region 'Fields

    End Structure

    Private Structure Results

#Region "Fields"

        Dim Result As Object
        Dim ResultType As SearchType

#End Region 'Fields

    End Structure

#End Region 'Nested Types

End Class

