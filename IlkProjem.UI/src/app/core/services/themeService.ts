import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class ThemeService {
  // Resimdeki palete sadık kalarak hazırlanan renk listesi
  private availableColors: string[] = [
    '#0f172a', '#10b981', '#84cc16', '#f59e0b', '#eab308',
    '#14b8a6', '#06b6d4', '#3b82f6', '#1d4ed8', '#8b5cf6',
    '#7c3aed', '#d946ef', '#ef4444'
  ];

  // Hata aldığın metot burası
  getAvailableColors(): string[] {
    return this.availableColors;
  }

  // Seçilen rengi uygulamaya ve localStorage'a kaydeder
  setThemeColor(color: string): void {
    // CSS değişkenini günceller (styles.css içindeki --accent-primary)
    document.documentElement.style.setProperty('--accent-primary', color);

    // Sayfa yenilendiğinde kaybolmaması için kaydeder
    localStorage.setItem('accent-color', color);
  }

  // Uygulama ilk açıldığında kaydedilen temaları yüklemek için
  loadSavedTheme(): void {
    const savedColor = localStorage.getItem('accent-color');
    if (savedColor) {
      this.setThemeColor(savedColor);
    }

    const savedTheme = localStorage.getItem('theme');
    if (savedTheme === 'dark') {
      document.body.classList.add('dark-theme');
    } else {
      document.body.classList.remove('dark-theme');
    }
  }
}