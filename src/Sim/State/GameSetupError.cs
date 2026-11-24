namespace Rts.Sim.State;

public enum GameSetupError
{
    NegativeResourceCount = 1,
    NoPlayers = 2,
    NoStarSystems = 3,
    NegativeId = 4,
    InvalidName = 5,
    DuplicatePlayerId = 6,
    DuplicateStarSystemId = 7,
    DuplicateWorldId = 8,
    DuplicateCountryId = 9,
    UnknownStarSystemOwner = 10,
    TooManyCountries = 11,
    InvalidControllerKind = 12,
    UnknownControllerPlayer = 13,
}
