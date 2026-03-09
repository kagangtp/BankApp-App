import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { TranslateModule } from '@ngx-translate/core';
import { ThemeService } from '../../core/services/themeService';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, TranslateModule],
  templateUrl: './settings.html',
  styleUrl: './settings.css'
})
export class Settings implements OnInit {
  isDarkMode = false;
  selectedColor = '#5d5fef'; // HTML'deki isimlendirme ile uyumlu
  availableColors: string[] = []; // HTML'deki döngü için

  private themeService = inject(ThemeService);

  ngOnInit() {
    // Renk paletini servisten al
    this.availableColors = this.themeService.getAvailableColors();

    // Kayıtlı tercihlerini yükle
    const savedColor = localStorage.getItem('accent-color');
    this.selectedColor = savedColor || '#5d5fef';
    this.themeService.setThemeColor(this.selectedColor);

    const savedTheme = localStorage.getItem('theme');
    this.isDarkMode = savedTheme === 'dark';
    this.applyTheme();
  }

  toggleTheme() {
    this.isDarkMode = !this.isDarkMode;
    localStorage.setItem('theme', this.isDarkMode ? 'dark' : 'light');
    this.applyTheme();
  }

  private applyTheme() {
    if (this.isDarkMode) {
      document.body.classList.add('dark-theme');
    } else {
      document.body.classList.remove('dark-theme');
    }
  }

  selectColor(color: string) {
    this.selectedColor = color; // Aktif rengi güncelle
    this.themeService.setThemeColor(color);
    localStorage.setItem('accent-color', color);
  }
}