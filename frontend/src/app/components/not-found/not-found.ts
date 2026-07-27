import { CommonModule } from '@angular/common';
import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { richHouseBrand } from '../../core/config/site-settings.config';

@Component({
  selector: 'app-not-found',
  imports: [CommonModule, RouterLink],
  templateUrl: './not-found.html',
  styleUrl: './not-found.css',
})
export class NotFound {
  readonly brand = richHouseBrand;
}
