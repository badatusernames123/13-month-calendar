namespace Calendar.Cli.Cli;

internal enum CliExitCode
{
    Success = 0,
    InvalidArguments = 2,
    NotFound = 3,
    ValidationError = 4,
    UnexpectedError = 10,
}
