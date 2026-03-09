import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BaseChartDirective } from 'ng2-charts';
import { ChartConfiguration, ChartOptions } from 'chart.js';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, BaseChartDirective],
  templateUrl: './dashboard.html',
  styleUrl: './dashboard.css'
})
export class Dashboard {
  public stats = {
    totalCustomers: 124,
    totalBalance: 2145830.50
  };

  today: Date = new Date();
  // Pie Chart Ayarları
  public pieChartData: ChartConfiguration<'pie'>['data'] = {
    labels: ['VIP Hesaplar', 'Standart Hesaplar', 'Yeni Hesaplar'],
    datasets: [{
      data: [500000, 1200000, 445830], // .NET'ten gelecek örnek veriler
      backgroundColor: ['#f6c23e', '#4e73df', '#1cc88a'], // Altın, Mavi, Yeşil
      hoverBackgroundColor: ['#dda20a', '#2e59d9', '#17a673'],
      hoverBorderColor: "rgba(234, 236, 244, 1)",
    }]
  };

  public pieChartOptions: ChartOptions<'pie'> = {
    responsive: true,
    maintainAspectRatio: false,
    plugins: {
      legend: {
        display: true,
        position: 'bottom', // Etiketler altta görünsün
      }
    }
  };
}