const watchDemoBtn = document.getElementById("watchDemoBtn");
const demoModal = document.getElementById("demoModal");

const openDemoModal = () => {
  if (!demoModal) return;

  demoModal.classList.add("active");
  demoModal.setAttribute("aria-hidden", "false");
  document.body.classList.add("modal-open");
};

const closeDemoModal = () => {
  if (!demoModal) return;

  demoModal.classList.remove("active");
  demoModal.setAttribute("aria-hidden", "true");
  document.body.classList.remove("modal-open");
};

if (watchDemoBtn) {
  watchDemoBtn.addEventListener("click", (e) => {
    e.preventDefault();
    openDemoModal();
  });
}

document.querySelectorAll("[data-close-demo]").forEach((element) => {
  element.addEventListener("click", closeDemoModal);
});

const featureDetails = {
  "event-creation": {
    icon: "fas fa-calendar-plus",
    title: "Event Creation",
    text: "Build a complete event page with title, description, images, date, location, capacity, and ticket settings from one clear form.",
    points: ["Add full event details", "Upload event images", "Set location and capacity"],
  },
  "ticket-types": {
    icon: "fas fa-ticket-alt",
    title: "Multiple Ticket Types",
    text: "Offer different ticket levels so attendees can choose the right experience, from free entry to VIP access.",
    points: ["Create VIP and regular tickets", "Control prices and quantities", "Support free ticket options"],
  },
  "qr-code": {
    icon: "fas fa-qrcode",
    title: "Secure QR Code",
    text: "Every confirmed booking receives a unique QR code that can be checked at the entrance to reduce fraud and duplicate use.",
    points: ["Unique code per ticket", "Fast entrance validation", "Prevents repeated scans"],
  },
  payments: {
    icon: "fas fa-credit-card",
    title: "Secure Payments",
    text: "Let attendees pay online through a smooth checkout flow with card support and automatic booking confirmation.",
    points: ["Online card payments", "Automatic confirmations", "Clear order summary"],
  },
  dashboard: {
    icon: "fas fa-chart-line",
    title: "Smart Dashboard",
    text: "Give organizers a focused dashboard to track ticket sales, revenue, attendance, and event performance in real time.",
    points: ["Track sales live", "Monitor attendance", "View event analytics"],
  },
  notifications: {
    icon: "fas fa-envelope-open-text",
    title: "Auto Notifications",
    text: "Send booking confirmations, reminders, and important updates automatically so attendees stay informed.",
    points: ["Email confirmations", "Event reminders", "Important update messages"],
  },
};

const featureModal = document.getElementById("featureModal");
const featureModalIcon = document.getElementById("featureModalIcon");
const featureModalTitle = document.getElementById("featureModalTitle");
const featureModalText = document.getElementById("featureModalText");
const featureModalList = document.getElementById("featureModalList");

const openFeatureModal = (featureKey) => {
  const feature = featureDetails[featureKey];
  if (!feature || !featureModal) return;

  featureModalIcon.innerHTML = `<i class="${feature.icon}"></i>`;
  featureModalTitle.textContent = feature.title;
  featureModalText.textContent = feature.text;
  featureModalList.innerHTML = feature.points
    .map((point) => `<li><i class="fas fa-check"></i>${point}</li>`)
    .join("");

  featureModal.classList.add("active");
  featureModal.setAttribute("aria-hidden", "false");
  document.body.classList.add("modal-open");
};

const closeFeatureModal = () => {
  if (!featureModal) return;

  featureModal.classList.remove("active");
  featureModal.setAttribute("aria-hidden", "true");
  document.body.classList.remove("modal-open");
};

document.querySelectorAll(".feature-link[data-feature]").forEach((link) => {
  link.addEventListener("click", function (e) {
    e.preventDefault();
    openFeatureModal(this.dataset.feature);
  });
});

document.querySelectorAll("[data-close-modal]").forEach((element) => {
  element.addEventListener("click", closeFeatureModal);
});

document.addEventListener("keydown", (e) => {
  if (e.key === "Escape") {
    closeFeatureModal();
    closeDemoModal();
  }
});

const counters = document.querySelectorAll(".counter");
const animateCounter = (counter) => {
  const target = +counter.getAttribute("data-target");
  const speed = 200;
  const update = () => {
    const count = +counter.innerText.replace(/,/g, "").replace("+", "");
    const inc = target / speed;
    if (count < target) {
      counter.innerText = Math.ceil(count + inc).toLocaleString();
      setTimeout(update, 15);
    } else {
      counter.innerText = target.toLocaleString() + "+";
    }
  };
  update();
};

if (counters.length > 0) {
  const observer = new IntersectionObserver((entries) => {
    entries.forEach((entry) => {
      if (entry.isIntersecting) {
        animateCounter(entry.target);
        observer.unobserve(entry.target);
      }
    });
  });
  counters.forEach((c) => observer.observe(c));
}

const navbar = document.querySelector(".navbar");
if (navbar && !navbar.classList.contains("scrolled")) {
  window.addEventListener("scroll", () => {
    navbar.classList.toggle("scrolled", window.scrollY > 50);
  });
}

const menuToggle = document.getElementById("menuToggle");
if (menuToggle) {
  menuToggle.addEventListener("click", function () {
    this.classList.toggle("active");
    document.querySelector(".nav-links").classList.toggle("active");
    document.querySelector(".nav-buttons").classList.toggle("active");
    document.body.classList.toggle("mobile-menu-open");
  });
}

const filterBtns = document.querySelectorAll(".filter-btn");
filterBtns.forEach((btn) => {
  btn.addEventListener("click", function () {
    filterBtns.forEach((b) => b.classList.remove("active"));
    this.classList.add("active");
  });
});

const revealElements = document.querySelectorAll(
  ".reveal, .feature-card, .step, .role-card, .event-card-large",
);
if (revealElements.length > 0) {
  const revealObserver = new IntersectionObserver(
    (entries) => {
      entries.forEach((e) => {
        if (e.isIntersecting) {
          e.target.classList.add("revealed");
          e.target.style.opacity = "1";
          e.target.style.transform = "translateY(0)";
        }
      });
    },
    { threshold: 0.1 },
  );

  revealElements.forEach((el, i) => {
    if (!el.classList.contains("revealed")) {
      el.style.opacity = "0";
      el.style.transform = "translateY(40px)";
      el.style.transition = `opacity 0.6s ease ${i * 0.05}s, transform 0.6s ease ${i * 0.05}s`;
    }
    revealObserver.observe(el);
  });
}

document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
  anchor.addEventListener("click", function (e) {
    const href = this.getAttribute("href");
    if (href === "#" || href.length <= 1) return;
    const target = document.querySelector(href);
    if (target) {
      e.preventDefault();
      target.scrollIntoView({ behavior: "smooth", block: "start" });
    }
  });
});

document.querySelectorAll(".btn-fav").forEach((btn) => {
  btn.addEventListener("click", function (e) {
    e.preventDefault();
    const icon = this.querySelector("i");
    if (icon.classList.contains("far")) {
      icon.classList.remove("far");
      icon.classList.add("fas");
      this.style.color = "#EF4444";
      this.style.background = "#FEE2E2";
    } else {
      icon.classList.remove("fas");
      icon.classList.add("far");
      this.style.color = "";
      this.style.background = "";
    }
  });
});

document.querySelectorAll("form").forEach((form) => {
  form.addEventListener("submit", function (e) {
    const requiredInputs = this.querySelectorAll("[required]");
    let valid = true;
    requiredInputs.forEach((input) => {
      if (!input.value.trim()) {
        input.style.borderColor = "#EF4444";
        valid = false;
      } else {
        input.style.borderColor = "";
      }
    });
    if (!valid) {
      e.preventDefault();
    }
  });
});

document.querySelectorAll(".newsletter").forEach((form) => {
  form.addEventListener("submit", function (e) {
    e.preventDefault();
    const input = this.querySelector("input");
    if (input.value.trim()) {
      const btn = this.querySelector("button");
      const original = btn.innerHTML;
      btn.innerHTML = '<i class="fas fa-check"></i>';
      input.value = "";
      setTimeout(() => {
        btn.innerHTML = original;
      }, 2000);
    }
  });
});

document.querySelectorAll("[data-feedback-carousel]").forEach((carousel) => {
  const cards = Array.from(carousel.querySelectorAll("[data-feedback-card]"));
  const pageSize = Number(carousel.dataset.pageSize || 5);
  const dotsContainer = carousel.querySelector("[data-feedback-dots]");
  const prevBtn = carousel.querySelector("[data-feedback-prev]");
  const nextBtn = carousel.querySelector("[data-feedback-next]");
  let page = 0;
  const totalPages = Math.max(1, Math.ceil(cards.length / pageSize));

  // Generate dynamic pagination dots
  if (dotsContainer) {
    dotsContainer.innerHTML = "";
    for (let i = 0; i < totalPages; i++) {
      const dot = document.createElement("button");
      dot.type = "button";
      dot.className = "carousel-dot" + (i === 0 ? " active" : "");
      dot.setAttribute("aria-label", `Go to slide ${i + 1}`);
      dot.addEventListener("click", () => {
        page = i;
        renderFeedbackPage();
      });
      dotsContainer.appendChild(dot);
    }
  }

  const renderFeedbackPage = () => {
    const grid = carousel.querySelector(".landing-feedback-grid-enhanced");
    if (grid) {
      grid.style.opacity = "0";
      grid.style.transition = "opacity 0.2s ease";
    }

    setTimeout(() => {
      cards.forEach((card, index) => {
        const visible = index >= page * pageSize && index < (page + 1) * pageSize;
        card.style.display = visible ? "flex" : "none";
      });

      // Update active dot indicators
      if (dotsContainer) {
        const dots = dotsContainer.querySelectorAll(".carousel-dot");
        dots.forEach((dot, index) => {
          dot.classList.toggle("active", index === page);
        });
      }

      if (prevBtn) {
        prevBtn.disabled = page === 0;
      }

      if (nextBtn) {
        nextBtn.disabled = page >= totalPages - 1;
      }

      if (grid) {
        grid.style.opacity = "1";
      }
    }, 180);
  };

  if (prevBtn) {
    prevBtn.addEventListener("click", () => {
      page = Math.max(0, page - 1);
      renderFeedbackPage();
    });
  }

  if (nextBtn) {
    nextBtn.addEventListener("click", () => {
      page = Math.min(totalPages - 1, page + 1);
      renderFeedbackPage();
    });
  }

  renderFeedbackPage();
});

// ===== Hero Background Slideshow =====
(() => {
    const slides = document.querySelectorAll('.hero-slide');
    const dots = document.querySelectorAll('.hero-slideshow-dot');
    if (slides.length < 2) return;

    let currentSlide = 0;
    const INTERVAL = 5000; // 5 seconds

    const goToSlide = (index) => {
        slides[currentSlide].classList.remove('active');
        dots[currentSlide].classList.remove('active');
        currentSlide = index;
        slides[currentSlide].classList.add('active');
        dots[currentSlide].classList.add('active');
    };

    // Auto-cycle
    let timer = setInterval(() => {
        goToSlide((currentSlide + 1) % slides.length);
    }, INTERVAL);

    // Dot click handlers
    dots.forEach((dot, i) => {
        dot.addEventListener('click', () => {
            clearInterval(timer);
            goToSlide(i);
            timer = setInterval(() => {
                goToSlide((currentSlide + 1) % slides.length);
            }, INTERVAL);
        });
    });
})();

window.addEventListener("load", () => {
  document.body.style.opacity = "0";
  document.body.style.transition = "opacity 0.4s ease";
  setTimeout(() => {
    document.body.style.opacity = "1";
  }, 50);
});
