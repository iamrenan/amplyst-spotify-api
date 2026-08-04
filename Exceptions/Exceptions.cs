
namespace amplyst_spotify_api.Exceptions;

public class ImportAlreadyInProgressException(string message) : Exception(message) { }

public class SpotifyAuthenticationException(string message) : Exception(message) { }