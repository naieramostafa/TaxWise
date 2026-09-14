import { Injectable, signal } from '@angular/core';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private themeKey = 'theme';
  theme = signal<'light' | 'dark'>(this.getInitial());

  constructor() {
    this.apply(this.theme());
  }

  private getInitial(): 'light' | 'dark' {
    const saved = localStorage.getItem(this.themeKey) as 'light' | 'dark' | null;
    if (saved === 'light' || saved === 'dark') return saved;
    return window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
  }

  toggle() {
    const next = this.theme() === 'light' ? 'dark' : 'light';
    this.theme.set(next);
    localStorage.setItem(this.themeKey, next);
    this.apply(next);
  }

  private apply(theme: 'light' | 'dark') {
    document.documentElement.setAttribute('data-theme', theme);
  }
}