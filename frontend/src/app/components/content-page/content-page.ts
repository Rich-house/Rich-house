import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

export interface ContentPageSection {
  title: string;
  paragraphs?: string[];
  bullets?: string[];
}

export interface ContentPageData {
  eyebrow: string;
  title: string;
  intro: string;
  sections: ContentPageSection[];
}

@Component({
  selector: 'app-content-page',
  imports: [CommonModule, RouterLink],
  templateUrl: './content-page.html',
  styleUrl: './content-page.css',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ContentPage {
  private readonly route = inject(ActivatedRoute);

  readonly content = this.route.snapshot.data['content'] as ContentPageData;
}
