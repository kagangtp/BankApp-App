using IlkProjem.BLL.Interfaces;
using IlkProjem.Core.Enums;
using IlkProjem.Core.Exceptions;
using IlkProjem.DAL.Interfaces;

namespace IlkProjem.BLL.Services;

public class CalculatorService : ICalculatorService
{
    private readonly ICustomerRepository _customerRepository;

    // Constructor Injection: Veritabanına erişim yetkisini içeri alıyoruz
    public CalculatorService(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    // 1. Basit Matematik 
    public decimal Sum(decimal a, decimal b) => a + b;
    public decimal Difference(decimal a, decimal b) => a - b;
    public decimal Multiply(decimal a, decimal b) => a * b;
    
    public decimal Divide(decimal a, decimal b)
    {
        if (b == 0) 
            throw new BusinessException(BusinessErrorCode.CalculatorDivideByZero, "A number cannot be divided by zero.");
            
        return a / b;
    }

    // 2. Toplam Müşteri Sayısını Getirir
    public async Task<int> GetTotalAccountCountAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        return customers.Count; // Listenin uzunluğunu verir
    }

    // 3. Tüm Müşterilerin Toplam Bakiyesini Hesaplar
    public async Task<decimal> GetTotalBalanceSumAsync()
    {
        var customers = await _customerRepository.GetAllAsync();
        
        return customers.Sum(c => c.Balance);
    }

}