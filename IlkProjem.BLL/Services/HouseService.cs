using IlkProjem.BLL.Interfaces;
using IlkProjem.Core.Dtos.HouseDtos;
using IlkProjem.Core.Dtos.FileDtos;
using IlkProjem.Core.Models;
using IlkProjem.Core.Utilities.Results;
using IlkProjem.DAL.Interfaces;

namespace IlkProjem.BLL.Services;

public class HouseService : IHouseService
{
    private readonly IHouseRepository _houseRepository;
    private readonly IFilesService _filesService;

    public HouseService(IHouseRepository houseRepository, IFilesService filesService)
    {
        _houseRepository = houseRepository;
        _filesService = filesService;
    }

    public async Task<IDataResult<int>> AddHouse(HouseCreateDto createDto, CancellationToken ct = default)
    {
        var house = new House
        {
            Address = createDto.Address,
            Description = createDto.Description,
            CustomerId = createDto.CustomerId
        };

        await _houseRepository.AddAsync(house, ct);
        return new SuccessDataResult<int>(house.Id, "Ev başarıyla eklendi.");
    }

    public async Task<IDataResult<List<HouseReadDto>>> GetHousesByCustomerId(int customerId, CancellationToken ct = default)
    {
        var houses = await _houseRepository.GetByCustomerIdAsync(customerId, ct);

        var dtos = new List<HouseReadDto>();
        foreach (var house in houses)
        {
            var images = await _filesService.GetByOwnerAsync("House", house.Id);
            dtos.Add(new HouseReadDto
            {
                Id = house.Id,
                Address = house.Address,
                Description = house.Description,
                CustomerId = house.CustomerId,
                Images = images
            });
        }

        return new SuccessDataResult<List<HouseReadDto>>(dtos);
    }

    public async Task<IDataResult<HouseReadDto>> GetHouseById(int id, CancellationToken ct = default)
    {
        var house = await _houseRepository.GetByIdAsync(id, ct);
        if (house == null) return new ErrorDataResult<HouseReadDto>("Ev bulunamadı.");

        var images = await _filesService.GetByOwnerAsync("House", house.Id);
        return new SuccessDataResult<HouseReadDto>(new HouseReadDto
        {
            Id = house.Id,
            Address = house.Address,
            Description = house.Description,
            CustomerId = house.CustomerId,
            Images = images
        });
    }

    public async Task<IResult> UpdateHouse(HouseUpdateDto updateDto, CancellationToken ct = default)
    {
        var house = await _houseRepository.GetByIdAsync(updateDto.Id, ct);
        if (house == null) return new ErrorResult("Ev bulunamadı.");

        house.Address = updateDto.Address;
        house.Description = updateDto.Description;

        var result = await _houseRepository.UpdateAsync(house, ct);
        return result ? new SuccessResult("Ev güncellendi.") : new ErrorResult("Güncelleme başarısız.");
    }

    public async Task<IResult> DeleteHouse(int id, CancellationToken ct = default)
    {
        var result = await _houseRepository.DeleteAsync(id, ct);
        return result
            ? new SuccessResult("Ev başarıyla silindi.")
            : new ErrorResult("Ev bulunamadı.");
    }
}
