using System.ComponentModel.DataAnnotations;
namespace Stockroom.Api.Contracts;
public sealed record CategoryInput([Required, StringLength(80, MinimumLength = 1)] string Name);
