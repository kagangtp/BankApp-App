using IlkProjem.BLL.Interfaces;
using IlkProjem.Core.Dtos.CarDtos;
using IlkProjem.Core.Dtos.FileDtos;
using IlkProjem.Core.Models;
using IlkProjem.Core.Utilities.Results;
using IlkProjem.DAL.Interfaces;
using FluentValidation;

namespace IlkProjem.BLL.Services;

public class CarService : ICarService
{
    private readonly ICarRepository _carRepository;
    private readonly IFilesService _filesService;
    private readonly IValidator<CarCreateDto> _createValidator;
    private readonly IValidator<CarUpdateDto> _updateValidator;

    public CarService(
        ICarRepository carRepository,
        IFilesService filesService,
        IValidator<CarCreateDto> createValidator,
        IValidator<CarUpdateDto> updateValidator)
    {
        _carRepository = carRepository;
        _filesService = filesService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    public async Task<IDataResult<int>> AddCar(CarCreateDto createDto, CancellationToken ct = default)
    {
        var validationResult = await _createValidator.ValidateAsync(createDto, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new ErrorDataResult<int>(errors);
        }

        var car = new Car
        {
            Plate = createDto.Plate,
            Description = createDto.Description,
            CustomerId = createDto.CustomerId
        };

        await _carRepository.AddAsync(car, ct);
        return new SuccessDataResult<int>(car.Id, "Araba başarıyla eklendi.");
    }

    public async Task<IDataResult<List<CarReadDto>>> GetCarsByCustomerId(int customerId, CancellationToken ct = default)
    {
        var cars = await _carRepository.GetByCustomerIdAsync(customerId, ct);

        var dtos = new List<CarReadDto>();
        foreach (var car in cars)
        {
            var images = await _filesService.GetByOwnerAsync("Car", car.Id);
            dtos.Add(new CarReadDto
            {
                Id = car.Id,
                Plate = car.Plate,
                Description = car.Description,
                CustomerId = car.CustomerId,
                Images = images
            });
        }

        return new SuccessDataResult<List<CarReadDto>>(dtos);
    }

    public async Task<IDataResult<CarReadDto>> GetCarById(int id, CancellationToken ct = default)
    {
        var car = await _carRepository.GetByIdAsync(id, ct);
        if (car == null) return new ErrorDataResult<CarReadDto>("Araba bulunamadı.");

        var images = await _filesService.GetByOwnerAsync("Car", car.Id);
        return new SuccessDataResult<CarReadDto>(new CarReadDto
        {
            Id = car.Id,
            Plate = car.Plate,
            Description = car.Description,
            CustomerId = car.CustomerId,
            Images = images
        });
    }

    public async Task<IResult> UpdateCar(CarUpdateDto updateDto, CancellationToken ct = default)
    {
        var validationResult = await _updateValidator.ValidateAsync(updateDto, ct);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(" | ", validationResult.Errors.Select(e => e.ErrorMessage));
            return new ErrorResult(errors);
        }

        var car = await _carRepository.GetByIdAsync(updateDto.Id, ct);
        if (car == null) return new ErrorResult("Araba bulunamadı.");

        car.Plate = updateDto.Plate;
        car.Description = updateDto.Description;

        var result = await _carRepository.UpdateAsync(car, ct);
        return result ? new SuccessResult("Araba güncellendi.") : new ErrorResult("Güncelleme başarısız.");
    }

    public async Task<IResult> DeleteCar(int id, CancellationToken ct = default)
    {
        var result = await _carRepository.DeleteAsync(id, ct);
        return result
            ? new SuccessResult("Araba başarıyla silindi.")
            : new ErrorResult("Araba bulunamadı.");
    }
}
